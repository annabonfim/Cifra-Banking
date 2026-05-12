using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cifra.Data;
using Cifra.DTOs;
using Cifra.Models;

namespace Cifra.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(AppDbContext db, ILogger<ClientesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("pf")]
    [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CriarPf([FromBody] CriarPessoaFisicaDto dto)
    {
        var agencia = await _db.Agencias.FindAsync(dto.AgenciaId);
        if (agencia is null)
            return NotFound(new { mensagem = $"Agência {dto.AgenciaId} não encontrada." });

        var jaExiste = await _db.PessoasFisicas
            .FirstOrDefaultAsync(p => p.Cpf == dto.Cpf);
        if (jaExiste is not null)
            return Conflict(new { mensagem = "CPF já cadastrado." });

        var pf = new PessoaFisica
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone ?? string.Empty,
            Cpf = dto.Cpf,
            DataNascimento = dto.DataNascimento,
            AgenciaId = dto.AgenciaId
        };

        _db.PessoasFisicas.Add(pf);
        await _db.SaveChangesAsync();

        _logger.LogInformation("PF cadastrada — Id={Id}, Cpf={Cpf}", pf.Id, pf.Cpf);

        return CreatedAtAction(nameof(GetById), new { id = pf.Id }, ToDto(pf));
    }

    [HttpPost("pj")]
    [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CriarPj([FromBody] CriarPessoaJuridicaDto dto)
    {
        var agencia = await _db.Agencias.FindAsync(dto.AgenciaId);
        if (agencia is null)
            return NotFound(new { mensagem = $"Agência {dto.AgenciaId} não encontrada." });

        var jaExiste = await _db.PessoasJuridicas
            .FirstOrDefaultAsync(p => p.Cnpj == dto.Cnpj);
        if (jaExiste is not null)
            return Conflict(new { mensagem = "CNPJ já cadastrado." });

        var pj = new PessoaJuridica
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone ?? string.Empty,
            Cnpj = dto.Cnpj,
            RazaoSocial = dto.RazaoSocial,
            AgenciaId = dto.AgenciaId
        };

        _db.PessoasJuridicas.Add(pj);
        await _db.SaveChangesAsync();

        _logger.LogInformation("PJ cadastrada — Id={Id}, Cnpj={Cnpj}", pj.Id, pj.Cnpj);

        return CreatedAtAction(nameof(GetById), new { id = pj.Id }, ToDto(pj));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null)
            return NotFound(new { mensagem = $"Cliente {id} não encontrado." });

        return Ok(ToDto(cliente));
    }

    private static ClienteResponseDto ToDto(Cliente c) => c switch
    {
        PessoaFisica pf => new ClienteResponseDto(
            pf.Id, "PF", pf.Nome, pf.Email, pf.Telefone, pf.Cpf, pf.DataNascimento,
            null, null, pf.AgenciaId, pf.DataCadastro),

        PessoaJuridica pj => new ClienteResponseDto(
            pj.Id, "PJ", pj.Nome, pj.Email, pj.Telefone, null, null,
            pj.Cnpj, pj.RazaoSocial, pj.AgenciaId, pj.DataCadastro),

        _ => throw new InvalidOperationException("Tipo de cliente desconhecido.")
    };
}