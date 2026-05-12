using Cifra.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Cifra.Tests;

public class ClientesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClientesIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    private async Task<Guid> CriarAgenciaAsync(string codigo = "0001")
    {
        var dto = new CriarAgenciaDto(codigo, "Agência Paulista", "Av. Paulista, 1000", "São Paulo", "SP");
        var resp = await _client.PostAsJsonAsync("/api/agencias", dto);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AgenciaResponseDto>();
        return body!.Id;
    }

    [Fact]
    public async Task CadastrarPF_ComDadosValidos_Retorna201()
    {
        var agenciaId = await CriarAgenciaAsync("0010");

        var dto = new CriarPessoaFisicaDto(
            Nome: "Anna Beatriz",
            Email: "anna@example.com",
            Telefone: "11999999999",
            Cpf: "12345678901",
            DataNascimento: new DateTime(2002, 5, 1),
            AgenciaId: agenciaId);

        var resp = await _client.PostAsJsonAsync("/api/clientes/pf", dto);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ClienteResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("PF", body!.Tipo);
        Assert.Equal("12345678901", body.Cpf);
    }

    [Fact]
    public async Task CadastrarPJ_ComDadosValidos_Retorna201()
    {
        var agenciaId = await CriarAgenciaAsync("0020");

        var dto = new CriarPessoaJuridicaDto(
            Nome: "Mitre Realty",
            Email: "ri@mitrerealty.com.br",
            Telefone: "1130000000",
            Cnpj: "07882930000162",
            RazaoSocial: "Mitre Realty Desenvolvimento Imobiliário S.A.",
            AgenciaId: agenciaId);

        var resp = await _client.PostAsJsonAsync("/api/clientes/pj", dto);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ClienteResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("PJ", body!.Tipo);
        Assert.Equal("07882930000162", body.Cnpj);
    }

    [Fact]
    public async Task CadastrarPF_CpfDuplicado_Retorna409()
    {
        var agenciaId = await CriarAgenciaAsync("0030");

        var dto = new CriarPessoaFisicaDto(
            "Cliente A", "a@example.com", null,
            "11122233344", new DateTime(1990, 1, 1), agenciaId);

        var primeira = await _client.PostAsJsonAsync("/api/clientes/pf", dto);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync("/api/clientes/pf", dto);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task CadastrarPJ_CnpjDuplicado_Retorna409()
    {
        var agenciaId = await CriarAgenciaAsync("0040");

        var dto = new CriarPessoaJuridicaDto(
            "Empresa X", "x@x.com", null,
            "99999999000199", "Empresa X Ltda", agenciaId);

        await _client.PostAsJsonAsync("/api/clientes/pj", dto);
        var segunda = await _client.PostAsJsonAsync("/api/clientes/pj", dto);

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task CadastrarPF_AgenciaInexistente_Retorna404()
    {
        var dto = new CriarPessoaFisicaDto(
            "Sem Agência", "ag@x.com", null,
            "55566677788", new DateTime(1985, 6, 15),
            AgenciaId: Guid.NewGuid());

        var resp = await _client.PostAsJsonAsync("/api/clientes/pf", dto);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetCliente_PorId_RetornaCliente()
    {
        var agenciaId = await CriarAgenciaAsync("0050");
        var criar = new CriarPessoaFisicaDto(
            "Get Test", "get@x.com", null,
            "11199988877", new DateTime(1995, 1, 1), agenciaId);

        var criada = await _client.PostAsJsonAsync("/api/clientes/pf", criar);
        var criado = await criada.Content.ReadFromJsonAsync<ClienteResponseDto>();

        var resp = await _client.GetAsync($"/api/clientes/{criado!.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
