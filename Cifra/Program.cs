using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Cifra.Data;
using Cifra.Messaging;
using Cifra.Services;
using System.Text.Json.Serialization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Serilog — console + arquivo
// ============================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/cifra-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// MVC + Swagger
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cifra API", Version = "v1" });
});

// ============================================================
// EF Core — Oracle FIAP (em Testing o factory registra com InMemory)
// ============================================================
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connStr = builder.Configuration.GetConnectionString("OracleFiap");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseOracle(connStr, x => x.MigrationsHistoryTable("CIFRA_EFMIGRATIONS")));
}

// ============================================================
// RabbitMQ — Publisher + Consumer
// ============================================================
var rabbitSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
                    ?? new RabbitMqSettings();
builder.Services.AddSingleton(rabbitSettings);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IContratacaoPublisher, ContratacaoPublisher>();
    builder.Services.AddHostedService<ContratacaoConsumer>();
}

builder.Services.AddScoped<IMaquinaDeCartaoProcessor, MaquinaDeCartaoProcessor>();

// ============================================================
// Health Checks
// ============================================================
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecksBuilder.AddDbContextCheck<AppDbContext>("oracle-db");
}

// ============================================================
// OpenTelemetry — tracing com exporter Console
// ============================================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Cifra"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// ============================================================
// Pipeline
// ============================================================
var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cifra API v1"));

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

try
{
    Log.Information("Iniciando Cifra");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha não recuperável ao iniciar a aplicação");
}
finally
{
    Log.CloseAndFlush();
}

// Necessário para WebApplicationFactory<Program> nos testes integrados
public partial class Program { }