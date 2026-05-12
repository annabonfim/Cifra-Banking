using Microsoft.AspNetCore.Mvc;
using Cifra.Data;
using Cifra.DTOs;
using Cifra.Messaging;
using Cifra.Models;

namespace Cifra.Controllers;

[ApiController]
[Route("api/contratacoes")]
public class ContratacoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IContratacaoPublisher _publisher;
    private readonly ILogger<ContratacoesController> _logger;

    public ContratacoesController(
        AppDbContext db,
        IContratacaoPublisher publisher,
        ILogger<ContratacoesController> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContratacaoResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Solicitar([FromBody] SolicitarContratacaoDto dto)
    {
        var cliente = await _db.Clientes.FindAsync(dto.ClienteId);
        if (cliente is null)
            return NotFound(new { mensagem = $"Cliente {dto.ClienteId} não encontrado." });

        var produto = await _db.Produtos.FindAsync(dto.ProdutoId);
        if (produto is null)
            return NotFound(new { mensagem = $"Produto {dto.ProdutoId} não encontrado." });

        if (!produto.Ativo)
            return BadRequest(new { mensagem = "Produto está inativo." });

        var tipoProduto = produto switch
        {
            MaquinaDeCartao => "MAQUINA_CARTAO",
            _ => "DESCONHECIDO"
        };

        var contratacao = new Contratacao
        {
            ClienteId = cliente.Id,
            ProdutoId = produto.Id,
            FaturamentoMensalEstimado = dto.FaturamentoMensalEstimado,
            Status = StatusContratacao.PENDENTE
        };

        _db.Contratacoes.Add(contratacao);
        await _db.SaveChangesAsync();

        var msg = new ContratacaoMessage(
            ContratacaoId: contratacao.Id,
            ClienteId: cliente.Id,
            ProdutoId: produto.Id,
            TipoProduto: tipoProduto,
            FaturamentoMensalEstimado: dto.FaturamentoMensalEstimado,
            DataSolicitacao: contratacao.DataSolicitacao);

        try
        {
            _publisher.Publish(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar mensagem para contratação {Id}", contratacao.Id);
        }

        return AcceptedAtAction(nameof(GetById), new { id = contratacao.Id }, ToDto(contratacao));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContratacaoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var contratacao = await _db.Contratacoes.FindAsync(id);
        if (contratacao is null)
            return NotFound(new { mensagem = $"Contratação {id} não encontrada." });

        return Ok(ToDto(contratacao));
    }

    private static ContratacaoResponseDto ToDto(Contratacao c) => new(
        c.Id, c.ClienteId, c.ProdutoId, c.Status, c.FaturamentoMensalEstimado,
        c.ScoreRisco, c.MdrAplicado, c.MotivoRecusa,
        c.DataSolicitacao, c.DataProcessamento);
}