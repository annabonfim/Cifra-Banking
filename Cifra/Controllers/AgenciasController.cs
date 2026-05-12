using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cifra.Data;
using Cifra.DTOs;
using Cifra.Models;

namespace Cifra.Controllers;

[ApiController]
[Route("api/agencias")]
public class AgenciasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AgenciasController> _logger;

    public AgenciasController(AppDbContext db, ILogger<AgenciasController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AgenciaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar([FromBody] CriarAgenciaDto dto)
    {
        var existente = await _db.Agencias.FirstOrDefaultAsync(a => a.Codigo == dto.Codigo);
        if (existente is not null)
            return Conflict(new { mensagem = $"Já existe agência com código {dto.Codigo}." });

        var agencia = new Agencia
        {
            Codigo = dto.Codigo,
            Nome = dto.Nome,
            Endereco = dto.Endereco ?? string.Empty,
            Cidade = dto.Cidade ?? string.Empty,
            Uf = dto.Uf ?? string.Empty
        };

        _db.Agencias.Add(agencia);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Agência cadastrada — Id={Id}, Codigo={Codigo}", agencia.Id, agencia.Codigo);

        return CreatedAtAction(nameof(GetById), new { id = agencia.Id }, ToDto(agencia));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgenciaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var agencia = await _db.Agencias.FindAsync(id);
        if (agencia is null)
            return NotFound(new { mensagem = $"Agência {id} não encontrada." });

        return Ok(ToDto(agencia));
    }

    private static AgenciaResponseDto ToDto(Agencia a) => new(
        a.Id, a.Codigo, a.Nome, a.Endereco, a.Cidade, a.Uf);
}