using Microsoft.EntityFrameworkCore;
using Cifra.Models;

namespace Cifra.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<PessoaFisica> PessoasFisicas => Set<PessoaFisica>();
    public DbSet<PessoaJuridica> PessoasJuridicas => Set<PessoaJuridica>();
    public DbSet<Agencia> Agencias => Set<Agencia>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MaquinaDeCartao> MaquinasDeCartao => Set<MaquinaDeCartao>();
    public DbSet<Contratacao> Contratacoes => Set<Contratacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Cliente (TPH) ----------
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("CIFRA_CLIENTE");
            e.HasKey(c => c.Id);
            e.Property(c => c.Nome).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).IsRequired().HasMaxLength(200);
            e.Property(c => c.Telefone).HasMaxLength(20);
            e.Property(c => c.DataCadastro).IsRequired();

            e.HasOne(c => c.Agencia)
                .WithMany(a => a.Clientes)
                .HasForeignKey(c => c.AgenciaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasDiscriminator<string>("TIPO_CLIENTE")
                .HasValue<PessoaFisica>("PF")
                .HasValue<PessoaJuridica>("PJ");
        });

        modelBuilder.Entity<PessoaFisica>(e =>
        {
            e.Property(p => p.Cpf).HasColumnName("CPF").HasMaxLength(11);
            e.Property(p => p.DataNascimento).HasColumnName("DATA_NASCIMENTO");
            e.HasIndex(p => p.Cpf).IsUnique();
        });

        modelBuilder.Entity<PessoaJuridica>(e =>
        {
            e.Property(p => p.Cnpj).HasColumnName("CNPJ").HasMaxLength(14);
            e.Property(p => p.RazaoSocial).HasColumnName("RAZAO_SOCIAL").HasMaxLength(200);
            e.HasIndex(p => p.Cnpj).IsUnique();
        });

        // ---------- Agência ----------
        modelBuilder.Entity<Agencia>(e =>
        {
            e.ToTable("CIFRA_AGENCIA");
            e.HasKey(a => a.Id);
            e.Property(a => a.Codigo).IsRequired().HasMaxLength(10);
            e.Property(a => a.Nome).IsRequired().HasMaxLength(200);
            e.Property(a => a.Endereco).HasMaxLength(300);
            e.Property(a => a.Cidade).HasMaxLength(120);
            e.Property(a => a.Uf).HasMaxLength(2);
            e.HasIndex(a => a.Codigo).IsUnique();
        });

        // ---------- Produto (TPH) ----------
        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("CIFRA_PRODUTO");
            e.HasKey(p => p.Id);
            e.Property(p => p.Nome).IsRequired().HasMaxLength(120);
            e.Property(p => p.Descricao).HasMaxLength(500);
            e.Property(p => p.Ativo).HasColumnType("NUMBER(1)");   // 👈 nova linha

            e.HasDiscriminator<string>("TIPO_PRODUTO")
                .HasValue<MaquinaDeCartao>("MAQUINA_CARTAO");
        });

        modelBuilder.Entity<MaquinaDeCartao>(e =>
        {
            e.Property(m => m.ModeloEquipamento).HasColumnName("MODELO_EQUIPAMENTO").HasMaxLength(50);
            e.Property(m => m.MdrBaseDebito).HasColumnName("MDR_BASE_DEBITO").HasPrecision(5, 2);
            e.Property(m => m.MdrBaseCreditoAVista).HasColumnName("MDR_BASE_CREDITO_VISTA").HasPrecision(5, 2);
            e.Property(m => m.MdrBaseCreditoParcelado).HasColumnName("MDR_BASE_CREDITO_PARC").HasPrecision(5, 2);
            e.Property(m => m.FaturamentoMinimoMensal).HasColumnName("FATURAMENTO_MIN").HasPrecision(14, 2);
        });

        // ---------- Contratacao ----------
        modelBuilder.Entity<Contratacao>(e =>
        {
            e.ToTable("CIFRA_CONTRATACAO");
            e.HasKey(c => c.Id);
            e.Property(c => c.Status).HasConversion<int>();
            e.Property(c => c.FaturamentoMensalEstimado).HasPrecision(14, 2);
            e.Property(c => c.MdrAplicado).HasMaxLength(200);
            e.Property(c => c.MotivoRecusa).HasMaxLength(300);

            e.HasOne(c => c.Cliente)
                .WithMany(cl => cl.Contratacoes)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Produto)
                .WithMany()
                .HasForeignKey(c => c.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}