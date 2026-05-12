namespace Cifra.Models;

/// <summary>
/// Produto bancário implementado nesta entrega.
/// Encapsula as taxas-base de MDR (Merchant Discount Rate)
/// por modalidade de transação, antes do ajuste por score de risco.
/// </summary>
public class MaquinaDeCartao : Produto
{
    public string ModeloEquipamento { get; set; } = "CIFRA-POS-A8";

    public decimal MdrBaseDebito { get; set; } = 1.50m;
    public decimal MdrBaseCreditoAVista { get; set; } = 2.99m;
    public decimal MdrBaseCreditoParcelado { get; set; } = 3.79m;

    public decimal FaturamentoMinimoMensal { get; set; } = 5000m;
}