namespace Cifra.DTOs;

public record AgenciaResponseDto(
    Guid Id,
    string Codigo,
    string Nome,
    string? Endereco,
    string? Cidade,
    string? Uf
);