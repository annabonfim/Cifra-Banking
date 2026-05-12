using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Cifra.Services;

namespace Cifra.Messaging;

/// <summary>
/// Consumer registrado como BackgroundService. Lê mensagens da fila
/// e roteia pelo discriminator TipoProduto. ACK manual: só confirma
/// após o processor concluir sem exceções. Em falha, NACK com requeue=true.
/// </summary>
public class ContratacaoConsumer : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContratacaoConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ContratacaoConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<ContratacaoConsumer> logger)
    {
        _settings = settings;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = factory.CreateConnection("cifra-consumer");
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) => await HandleAsync(ea, stoppingToken);

        _channel.BasicConsume(
            queue: _settings.QueueName,
            autoAck: false,           // <-- ACK MANUAL (requisito 3.2)
            consumer: consumer);

        _logger.LogInformation("ContratacaoConsumer iniciado — escutando fila {Queue}", _settings.QueueName);

        return Task.CompletedTask;
    }

    private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        ContratacaoMessage? msg = null;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            msg = JsonSerializer.Deserialize<ContratacaoMessage>(json);

            if (msg is null)
                throw new InvalidOperationException("Payload nulo ou inválido.");

            _logger.LogInformation(
                "Mensagem recebida — ContratacaoId={Id}, TipoProduto={Tipo}",
                msg.ContratacaoId, msg.TipoProduto);

            using var scope = _serviceProvider.CreateScope();

            switch (msg.TipoProduto)
            {
                case "MAQUINA_CARTAO":
                    var processor = scope.ServiceProvider.GetRequiredService<IMaquinaDeCartaoProcessor>();
                    await processor.ProcessarAsync(msg, ct);
                    break;

                default:
                    _logger.LogWarning("TipoProduto {Tipo} não tratado — descartando mensagem (ACK).",
                        msg.TipoProduto);
                    break;
            }

            _channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao processar mensagem {Id} — fazendo NACK com requeue=true.",
                msg?.ContratacaoId);

            _channel!.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}