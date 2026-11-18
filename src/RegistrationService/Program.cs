using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Compose injects these settings so secrets and environment-specific values are not hard-coded in source.
builder.Services.AddDbContext<RegistrationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Registrations")
        ?? throw new InvalidOperationException("ConnectionStrings:Registrations is required.")));
builder.Services.AddSingleton<RegistrationEventPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();

// EnsureCreated is acceptable for a disposable training lab; production systems should use EF migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
    var attempts = 0;
    while (true)
    {
        try { db.Database.EnsureCreated(); break; }
        catch when (++attempts < 40) { await Task.Delay(TimeSpan.FromSeconds(3)); }
    }
}

app.MapGet("/health", () => Results.Ok(new { service = "registration", status = "healthy" }));
app.MapGet("/api/registrations", async (RegistrationDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Registrations.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)));

app.MapGet("/api/registrations/{id:int}", async Task<IResult> (int id, RegistrationDbContext db, CancellationToken ct) =>
{
    var item = await db.Registrations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    return item is null ? Results.NotFound(new { message = "Registration not found." }) : Results.Ok(item);
});

app.MapPost("/api/registrations", async Task<IResult> (CreateRegistrationRequest request, RegistrationDbContext db,
    RegistrationEventPublisher publisher, CancellationToken ct) =>
{
    // Validate incoming data at the service boundary.
    if (string.IsNullOrWhiteSpace(request.CandidateNumber) || string.IsNullOrWhiteSpace(request.CandidateName)
        || string.IsNullOrWhiteSpace(request.ExamSession) || request.Subjects is null || request.Subjects.Count == 0)
        return Results.BadRequest(new { message = "Candidate number, candidate name, exam session, and at least one subject are required." });

    var candidateNumber = request.CandidateNumber.Trim().ToUpperInvariant();
    var session = request.ExamSession.Trim();
    if (await db.Registrations.AnyAsync(x => x.CandidateNumber == candidateNumber && x.ExamSession == session, ct))
        return Results.Conflict(new { message = "This candidate already has a registration for that exam session." });

    var subjects = request.Subjects.Select(s => s.Trim()).Where(s => s.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    if (subjects.Length == 0) return Results.BadRequest(new { message = "Provide at least one non-empty subject." });

    var item = new ExamRegistration
    {
        CandidateNumber = candidateNumber, CandidateName = request.CandidateName.Trim(),
        ExamSession = session, Subjects = string.Join(",", subjects), CreatedAtUtc = DateTime.UtcNow
    };
    db.Registrations.Add(item);
    await db.SaveChangesAsync(ct);

    // Simple publish-after-save demo. A transactional outbox is the production solution to the dual-write gap.
    await publisher.PublishAsync(new RegistrationCreatedEvent(item.Id, item.CandidateNumber, item.CandidateName,
        item.ExamSession, item.Subjects, item.CreatedAtUtc), ct);
    return Results.Created($"/api/registrations/{item.Id}", item);
});

app.Run();

public sealed record CreateRegistrationRequest(string CandidateNumber, string CandidateName, string ExamSession, List<string> Subjects);
public sealed record RegistrationCreatedEvent(int RegistrationId, string CandidateNumber, string CandidateName,
    string ExamSession, string Subjects, DateTime CreatedAtUtc);

public sealed class ExamRegistration
{
    public int Id { get; set; }
    public string CandidateNumber { get; set; } = "";
    public string CandidateName { get; set; } = "";
    public string ExamSession { get; set; } = "";
    public string Subjects { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
{
    public DbSet<ExamRegistration> Registrations => Set<ExamRegistration>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExamRegistration>().HasIndex(x => new { x.CandidateNumber, x.ExamSession }).IsUnique();
        modelBuilder.Entity<ExamRegistration>().Property(x => x.CandidateNumber).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<ExamRegistration>().Property(x => x.CandidateName).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<ExamRegistration>().Property(x => x.ExamSession).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<ExamRegistration>().Property(x => x.Subjects).HasMaxLength(500).IsRequired();
    }
}

public sealed class RegistrationEventPublisher(IConfiguration configuration)
{
    public Task PublishAsync(RegistrationCreatedEvent message, CancellationToken ct)
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
        channel.QueueDeclare("registration.created", durable: true, exclusive: false, autoDelete: false);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        channel.BasicPublish("", "registration.created", properties, body);
        return Task.CompletedTask;
    }
}