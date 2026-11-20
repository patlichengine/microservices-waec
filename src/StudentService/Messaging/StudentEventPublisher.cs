using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using StudentService.DTOs;

namespace StudentService.Messaging;

public sealed record StudentCreatedEvent(
    Guid StudentId,
    string StudentNumber,
    string FullName,
    string EmailAddress,
    string SchoolName,
    DateTime CreatedAtUtc);

public interface IStudentEventPublisher
{
    Task PublishStudentCreatedEventAsync(StudentResponseDto student, CancellationToken ct = default);
}

public sealed class StudentEventPublisher(
    IConfiguration configuration,
    ILogger<StudentEventPublisher> logger) : IStudentEventPublisher
{
    private const string QueueName = "student.created";

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

        channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        var eventPayload = new StudentCreatedEvent(
            StudentId: student.Id,
            StudentNumber: student.StudentNumber,
            FullName: $"{student.FirstName} {student.LastName}".Trim(),
            EmailAddress: student.EmailAddress,
            SchoolName: student.SchoolName,
            CreatedAtUtc: DateTime.UtcNow);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventPayload));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: QueueName,
            basicProperties: properties,
            body: body);

        logger.LogInformation(
            "Published {QueueName} event for Student {StudentNumber} ({StudentId})",
            QueueName,
            student.StudentNumber,
            student.Id);

        return Task.CompletedTask;
    }
}
