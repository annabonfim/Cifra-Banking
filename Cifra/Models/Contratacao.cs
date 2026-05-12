namespace Cifra.Models;

public class Contratacao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public Guid ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public StatusContratacao Status { get; set; } = StatusContratacao.PENDENTE;

    public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataProcessamento { get; set; }

    public decimal FaturamentoMensalEstimado { get; set; }
    public int? ScoreRisco { get; set; }
    public string? MdrAplicado { get; set; }
    public string? MotivoRecusa { get; set; }
}