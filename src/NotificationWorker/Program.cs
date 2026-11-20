using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<NotificationConsumer>();

await builder.Build().RunAsync();

public sealed class NotificationConsumer(
    ILogger<NotificationConsumer> logger,
    IConfiguration configuration)
    : BackgroundService
{
    private const string RegistrationQueue = "registration.created";
    private const string StudentQueue = "student.created";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "rabbitmq",
            UserName = configuration["RabbitMq:User"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        // Keep retrying until RabbitMQ becomes available or the application stops.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                logger.LogInformation("Connected to RabbitMQ.");

                channel.BasicQos(0, 1, false);

                // 1. Registration Created Consumer
                channel.QueueDeclare(
                    queue: RegistrationQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                var registrationConsumer = new AsyncEventingBasicConsumer(channel);
                registrationConsumer.Received += async (_, args) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(args.Body.ToArray());

                        var message =
                            JsonSerializer.Deserialize<RegistrationCreatedEvent>(json, JsonOptions)
                            ?? throw new InvalidOperationException("Invalid registration event.");

                        logger.LogInformation(
                            "NOTIFICATION: Registration {RegistrationId} for {CandidateName} ({CandidateNumber}), session {ExamSession}, subjects {Subjects}",
                            message.RegistrationId,
                            message.CandidateName,
                            message.CandidateNumber,
                            message.ExamSession,
                            message.Subjects);

                        channel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError(ex, "Malformed registration event payload; discarding message.");
                        channel.BasicNack(args.DeliveryTag, false, requeue: false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Could not process registration event; requeueing.");
                        channel.BasicNack(args.DeliveryTag, false, requeue: true);
                    }

                    await Task.CompletedTask;
                };

                channel.BasicConsume(
                    queue: RegistrationQueue,
                    autoAck: false,
                    consumer: registrationConsumer);

                // 2. Student Created Consumer
                channel.QueueDeclare(
                    queue: StudentQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                var studentConsumer = new AsyncEventingBasicConsumer(channel);
                studentConsumer.Received += async (_, args) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(args.Body.ToArray());

                        var message =
                            JsonSerializer.Deserialize<StudentCreatedEvent>(json, JsonOptions)
                            ?? throw new InvalidOperationException("Invalid student event.");

                        logger.LogInformation(
                            "NOTIFICATION: Student {FullName} ({StudentNumber}) created for school {SchoolName} ({EmailAddress})",
                            message.FullName,
                            message.StudentNumber,
                            message.SchoolName,
                            message.EmailAddress);

                        channel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError(ex, "Malformed student event payload; discarding message.");
                        channel.BasicNack(args.DeliveryTag, false, requeue: false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Could not process student event; requeueing.");
                        channel.BasicNack(args.DeliveryTag, false, requeue: true);
                    }

                    await Task.CompletedTask;
                };

                channel.BasicConsume(
                    queue: StudentQueue,
                    autoAck: false,
                    consumer: studentConsumer);

                logger.LogInformation(
                    "Listening for {RegistrationQueue} and {StudentQueue} events.",
                    RegistrationQueue,
                    StudentQueue);

                // Keep the connection and channel alive.
                while (!stoppingToken.IsCancellationRequested &&
                       connection.IsOpen &&
                       channel.IsOpen)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Notification Worker connection failed. Retrying in 5 seconds.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

public sealed record RegistrationCreatedEvent(
    int RegistrationId,
    string CandidateNumber,
    string CandidateName,
    string ExamSession,
    string Subjects,
    DateTime CreatedAtUtc);

public sealed record StudentCreatedEvent(
    Guid StudentId,
    string StudentNumber,
    string FullName,
    string EmailAddress,
    string SchoolName,
    DateTime CreatedAtUtc);
