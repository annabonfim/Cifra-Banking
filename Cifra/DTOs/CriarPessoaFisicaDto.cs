using System.ComponentModel.DataAnnotations;

namespace Cifra.DTOs;

public record CriarPessoaFisicaDto(
    [Required] string Nome,
    [Required, EmailAddress] string Email,
    string? Telefone,
    [Required, StringLength(11, MinimumLength = 11)] string Cpf,
    [Required] DateTime DataNascimento,
    [Required] Guid AgenciaId
);