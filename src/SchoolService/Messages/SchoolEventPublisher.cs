using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SchoolService.DTOs;

namespace SchoolService.Messages;

public interface ISchoolEventPublisher
{
    Task PublishSchoolCreatedEventAsync(SchoolResponseDto school, CancellationToken ct = default);
}

public sealed class SchoolEventPublisher(
    IConfiguration configuration,
    ILogger<SchoolEventPublisher> logger) : ISchoolEventPublisher
{
    private const string ExchangeName = "school.created";

    public Task PublishSchoolCreatedEventAsync(SchoolResponseDto school, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "rabbitmq",
            UserName = configuration["RabbitMq:User"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true);

        var message = JsonSerializer.Serialize(school);
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: string.Empty,
            basicProperties: null,
            body: body);

        logger.LogInformation("Published school.created event for School ID {SchoolId}", school.Id);

        return Task.CompletedTask;
    }
}
