using Cifra.Models;

namespace Cifra.DTOs;

public record ContratacaoResponseDto(
    Guid Id,
    Guid ClienteId,
    Guid ProdutoId,
    StatusContratacao Status,
    decimal FaturamentoMensalEstimado,
    int? ScoreRisco,
    string? MdrAplicado,
    string? MotivoRecusa,
    DateTime DataSolicitacao,
    DateTime? DataProcessamento
);