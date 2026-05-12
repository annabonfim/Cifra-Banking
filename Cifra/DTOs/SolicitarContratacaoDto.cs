using System.ComponentModel.DataAnnotations;

namespace Cifra.DTOs;

public record SolicitarContratacaoDto(
    [Required] Guid ClienteId,
    [Required] Guid ProdutoId,
    [Required, Range(0, double.MaxValue)] decimal FaturamentoMensalEstimado
);