using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Cifra.Messaging;

public interface IContratacaoPublisher
{
    void Publish(ContratacaoMessage message);
}

public class ContratacaoPublisher : IContratacaoPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<ContratacaoPublisher> _logger;
    private readonly IConnection _connection;

    public ContratacaoPublisher(RabbitMqSettings settings, ILogger<ContratacaoPublisher> logger)
    {
        _settings = settings;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            DispatchConsumersAsync = false
        };

        _connection = factory.CreateConnection("cifra-publisher");
        _logger.LogInformation("Conexão com RabbitMQ estabelecida em {Host}:{Port}",
            settings.HostName, settings.Port);
    }

    public void Publish(ContratacaoMessage message)
    {
        using var channel = _connection.CreateModel();

        channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = message.ContratacaoId.ToString();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _settings.QueueName,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "Mensagem publicada na fila {Queue} — ContratacaoId={Id}, TipoProduto={Tipo}",
            _settings.QueueName, message.ContratacaoId, message.TipoProduto);
    }

    public void Dispose()
    {
        if (_connection.IsOpen) _connection.Close();
        _connection.Dispose();
    }
}