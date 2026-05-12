namespace Cifra.DTOs;

public record ClienteResponseDto(
    Guid Id,
    string Tipo,
    string Nome,
    string Email,
    string? Telefone,
    string? Cpf,
    DateTime? DataNascimento,
    string? Cnpj,
    string? RazaoSocial,
    Guid AgenciaId,
    DateTime DataCadastro
);