namespace Cifra.Messaging;

/// <summary>
/// Payload publicado na fila quando uma contratação é solicitada.
/// O campo TipoProduto funciona como discriminator para que o consumer
/// decida qual processor invocar (estratégia "fila única com discriminator").
/// </summary>
public record ContratacaoMessage(
    Guid ContratacaoId,
    Guid ClienteId,
    Guid ProdutoId,
    string TipoProduto,
    decimal FaturamentoMensalEstimado,
    DateTime DataSolicitacao
);