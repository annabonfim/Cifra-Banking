using Cifra.Data;
using Cifra.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cifra.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IContratacaoPublisher> PublisherMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("TestDb"));

            services.AddSingleton<IContratacaoPublisher>(_ => PublisherMock.Object);
        });
    }

    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db;
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}