using System.ComponentModel.DataAnnotations;

namespace Cifra.DTOs;

public record CriarAgenciaDto(
    [Required] string Codigo,
    [Required] string Nome,
    string? Endereco,
    string? Cidade,
    string? Uf
);