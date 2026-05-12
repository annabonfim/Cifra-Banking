using System.ComponentModel.DataAnnotations;

namespace Cifra.DTOs;

public record CriarPessoaJuridicaDto(
    [Required] string Nome,
    [Required, EmailAddress] string Email,
    string? Telefone,
    [Required, StringLength(14, MinimumLength = 14)] string Cnpj,
    [Required] string RazaoSocial,
    [Required] Guid AgenciaId
);