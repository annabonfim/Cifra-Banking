using Cifra.Data;
using Cifra.Messaging;
using Cifra.Models;

namespace Cifra.Services;

public interface IMaquinaDeCartaoProcessor
{
    Task ProcessarAsync(ContratacaoMessage mensagem, CancellationToken ct);
}

public class MaquinaDeCartaoProcessor : IMaquinaDeCartaoProcessor
{
    private readonly AppDbContext _db;
    private readonly ILogger<MaquinaDeCartaoProcessor> _logger;

    public MaquinaDeCartaoProcessor(AppDbContext db, ILogger<MaquinaDeCartaoProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessarAsync(ContratacaoMessage msg, CancellationToken ct)
    {
        var contratacao = await _db.Contratacoes.FindAsync(new object?[] { msg.ContratacaoId }, ct);
        if (contratacao is null)
        {
            _logger.LogWarning("Contratação {Id} não encontrada.", msg.ContratacaoId);
            return;
        }

        var produto = await _db.Produtos.FindAsync(new object?[] { msg.ProdutoId }, ct) as MaquinaDeCartao;
        if (produto is null)
        {
            contratacao.Status = StatusContratacao.RECUSADA;
            contratacao.MotivoRecusa = "Produto MaquinaDeCartao não encontrado.";
            contratacao.DataProcessamento = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        // 1) Score de risco — sigmóide do faturamento.
        var score = CalcularScore(msg.FaturamentoMensalEstimado);
        contratacao.ScoreRisco = score;

        // 2) Simula tempo de ativação do equipamento físico
        await Task.Delay(500, ct);

        // 3) Regras de recusa
        if (msg.FaturamentoMensalEstimado < produto.FaturamentoMinimoMensal)
        {
            contratacao.Status = StatusContratacao.RECUSADA;
            contratacao.MotivoRecusa =
                $"Faturamento mensal estimado (R$ {msg.FaturamentoMensalEstimado:N2}) inferior ao mínimo " +
                $"exigido (R$ {produto.FaturamentoMinimoMensal:N2}).";
        }
        else if (score < 40)
        {
            contratacao.Status = StatusContratacao.RECUSADA;
            contratacao.MotivoRecusa = $"Score de risco insuficiente ({score}/100).";
        }
        else
        {
            // 4) MDR final com ajuste pelo score
            var ajuste = (decimal)((75 - score) * 0.01);

            var debito = Math.Max(0.5m, produto.MdrBaseDebito + ajuste);
            var creditoVista = Math.Max(1.0m, produto.MdrBaseCreditoAVista + ajuste);
            var creditoParc = Math.Max(1.5m, produto.MdrBaseCreditoParcelado + ajuste);

            contratacao.MdrAplicado =
                $"DEBITO={debito:F2}%;CREDITO_A_VISTA={creditoVista:F2}%;CREDITO_PARCELADO={creditoParc:F2}%";
            contratacao.Status = StatusContratacao.APROVADA;
        }

        contratacao.DataProcessamento = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contratação {Id} processada — Status={Status}, Score={Score}, MDR={Mdr}",
            contratacao.Id, contratacao.Status, contratacao.ScoreRisco, contratacao.MdrAplicado);
    }

    private static int CalcularScore(decimal faturamento)
    {
        if (faturamento <= 0) return 0;
        var raw = 100.0 * (1.0 - Math.Exp(-(double)faturamento / 25000.0));
        return Math.Clamp((int)Math.Round(raw), 0, 100);
    }
}