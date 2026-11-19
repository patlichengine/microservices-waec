using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using StudentService.DTOs;

namespace StudentService.Messaging;

public interface IStudentEventPublisher
{
    Task PublishStudentCreatedEventAsync(StudentResponseDto student, CancellationToken ct = default);
}

public sealed class StudentEventPublisher(
    IConfiguration configuration,
    ILogger<StudentEventPublisher> logger) : IStudentEventPublisher
{
    private const string ExchangeName = "student.created";

    public Task PublishStudentCreatedEventAsync(StudentResponseDto student, CancellationToken ct = default)
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

        var message = JsonSerializer.Serialize(student);
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: string.Empty,
            basicProperties: null,
            body: body);

        logger.LogInformation("Published student.created event for Student ID {StudentId}", student.Id);

        return Task.CompletedTask;
    }
}
