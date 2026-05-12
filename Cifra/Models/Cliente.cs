namespace Cifra.Models;

public abstract class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public Guid AgenciaId { get; set; }
    public Agencia? Agencia { get; set; }

    public ICollection<Contratacao> Contratacoes { get; set; } = new List<Contratacao>();
}