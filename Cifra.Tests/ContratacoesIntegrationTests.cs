using Cifra.Data;
using Cifra.DTOs;
using Cifra.Messaging;
using Cifra.Models;
using Cifra.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Cifra.Tests;

public class ContratacoesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContratacoesIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _factory.PublisherMock.Invocations.Clear();
        _client = factory.CreateClient();
    }

    private async Task<(Guid clienteId, Guid produtoId)> ArrangeAsync()
    {
        var agenciaResp = await _client.PostAsJsonAsync("/api/agencias",
            new CriarAgenciaDto("9000", "Agência Teste", "Rua X", "São Paulo", "SP"));
        var agencia = await agenciaResp.Content.ReadFromJsonAsync<AgenciaResponseDto>();

        var clienteResp = await _client.PostAsJsonAsync("/api/clientes/pf",
            new CriarPessoaFisicaDto("João Teste", "joao@x.com", null,
                "99988877766", new DateTime(1990, 1, 1), agencia!.Id));
        var cliente = await clienteResp.Content.ReadFromJsonAsync<ClienteResponseDto>();

        Guid produtoId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var produto = new MaquinaDeCartao
            {
                Nome = "Maquininha Cifra",
                Descricao = "Maquininha de cartão padrão",
                ModeloEquipamento = "CIFRA-POS-A8"
            };
            db.MaquinasDeCartao.Add(produto);
            await db.SaveChangesAsync();
            produtoId = produto.Id;
        }

        return (cliente!.Id, produtoId);
    }

    [Fact]
    public async Task SolicitarContratacao_Valida_Retorna202EPublicaNaFila()
    {
        var (clienteId, produtoId) = await ArrangeAsync();

        var dto = new SolicitarContratacaoDto(clienteId, produtoId, FaturamentoMensalEstimado: 30000m);

        var resp = await _client.PostAsJsonAsync("/api/contratacoes", dto);

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<ContratacaoResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(StatusContratacao.PENDENTE, body!.Status);

        _factory.PublisherMock.Verify(
            p => p.Publish(It.Is<ContratacaoMessage>(m =>
                m.ContratacaoId == body.Id &&
                m.TipoProduto == "MAQUINA_CARTAO" &&
                m.FaturamentoMensalEstimado == 30000m)),
            Times.Once);
    }

    [Fact]
    public async Task SolicitarContratacao_ClienteInexistente_Retorna404()
    {
        var (_, produtoId) = await ArrangeAsync();

        var dto = new SolicitarContratacaoDto(
            ClienteId: Guid.NewGuid(),
            ProdutoId: produtoId,
            FaturamentoMensalEstimado: 10000m);

        var resp = await _client.PostAsJsonAsync("/api/contratacoes", dto);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task SolicitarContratacao_ProdutoInexistente_Retorna404()
    {
        var (clienteId, _) = await ArrangeAsync();

        var dto = new SolicitarContratacaoDto(
            ClienteId: clienteId,
            ProdutoId: Guid.NewGuid(),
            FaturamentoMensalEstimado: 10000m);

        var resp = await _client.PostAsJsonAsync("/api/contratacoes", dto);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task ConsultarStatus_AposProcessamento_RetornaAprovada()
    {
        var (clienteId, produtoId) = await ArrangeAsync();

        var solicitar = await _client.PostAsJsonAsync("/api/contratacoes",
            new SolicitarContratacaoDto(clienteId, produtoId, 30000m));
        var contratacao = await solicitar.Content.ReadFromJsonAsync<ContratacaoResponseDto>();

        using (var scope = _factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IMaquinaDeCartaoProcessor>();
            await processor.ProcessarAsync(
                new ContratacaoMessage(contratacao!.Id, clienteId, produtoId,
                    "MAQUINA_CARTAO", 30000m, contratacao.DataSolicitacao),
                CancellationToken.None);
        }

        var get = await _client.GetAsync($"/api/contratacoes/{contratacao!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var atualizada = await get.Content.ReadFromJsonAsync<ContratacaoResponseDto>();
        Assert.Equal(StatusContratacao.APROVADA, atualizada!.Status);
        Assert.NotNull(atualizada.MdrAplicado);
        Assert.NotNull(atualizada.ScoreRisco);
        Assert.NotNull(atualizada.DataProcessamento);
    }

    [Fact]
    public async Task ProcessarContratacao_FaturamentoAbaixoDoMinimo_RecusaContratacao()
    {
        var (clienteId, produtoId) = await ArrangeAsync();

        var solicitar = await _client.PostAsJsonAsync("/api/contratacoes",
            new SolicitarContratacaoDto(clienteId, produtoId, FaturamentoMensalEstimado: 1000m));
        var contratacao = await solicitar.Content.ReadFromJsonAsync<ContratacaoResponseDto>();

        using (var scope = _factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IMaquinaDeCartaoProcessor>();
            await processor.ProcessarAsync(
                new ContratacaoMessage(contratacao!.Id, clienteId, produtoId,
                    "MAQUINA_CARTAO", 1000m, contratacao.DataSolicitacao),
                CancellationToken.None);
        }

        var get = await _client.GetAsync($"/api/contratacoes/{contratacao!.Id}");
        var atualizada = await get.Content.ReadFromJsonAsync<ContratacaoResponseDto>();

        Assert.Equal(StatusContratacao.RECUSADA, atualizada!.Status);
        Assert.NotNull(atualizada.MotivoRecusa);
    }

    [Fact]
    public async Task HealthCheck_RetornaHealthy()
    {
        var resp = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}