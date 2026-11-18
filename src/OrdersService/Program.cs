using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);
// Docker overrides Catalog:BaseUrl with http://catalog:8080 through environment configuration.
var catalogBaseUrl = builder.Configuration["Catalog:BaseUrl"] ?? "http://localhost:8081";
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(catalogBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddSingleton<OrderStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();

app.MapGet("/health", () => Results.Ok(new { service = "orders", status = "healthy" }));
app.MapGet("/api/orders", (OrderStore store) => Results.Ok(store.GetAll()));
app.MapGet("/api/orders/{id:int}", (int id, OrderStore store) =>
{
    var order = store.Get(id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});
app.MapPost("/api/orders", async (CreateOrderRequest request, CatalogClient catalog, OrderStore store, CancellationToken ct) =>
{
    if (request.SubjectId <= 0 || string.IsNullOrWhiteSpace(request.CandidateName) ||
        string.IsNullOrWhiteSpace(request.ExamSession))
        return Results.BadRequest(new { error = "SubjectId, candidateName and examSession are required." });

    // Orders asks Catalog to validate the subject instead of accessing Catalog's private data.
    var lookup = await catalog.GetSubjectAsync(request.SubjectId, ct);
    if (lookup.Status == CatalogLookupStatus.NotFound)
        return Results.BadRequest(new { error = $"Subject {request.SubjectId} does not exist." });
    if (lookup.Status == CatalogLookupStatus.Unavailable)
        return Results.Problem(title: "Catalog service unavailable",
            detail: "The order was not created. Retry after Catalog is available.",
            statusCode: StatusCodes.Status503ServiceUnavailable);

    var order = store.Add(request, lookup.Subject!.Name);
    return Results.Created($"/api/orders/{order.Id}", order);
});
app.Run();

public sealed record CreateOrderRequest(int SubjectId, string CandidateName, string ExamSession);
public sealed record ExamOrder(int Id, int SubjectId, string SubjectName, string CandidateName, string ExamSession, DateTimeOffset CreatedAt);
public enum CatalogLookupStatus { Found, NotFound, Unavailable }
public sealed record CatalogLookup(CatalogLookupStatus Status, CatalogSubject? Subject = null);
public sealed record CatalogSubject(int Id, string Code, string Name, bool Active);

public sealed class CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger)
{
    public async Task<CatalogLookup> GetSubjectAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/catalog/subjects/{id}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new CatalogLookup(CatalogLookupStatus.NotFound);
            response.EnsureSuccessStatusCode();
            var subject = await response.Content.ReadFromJsonAsync<CatalogSubject>(cancellationToken: cancellationToken);
            return subject is null ? new CatalogLookup(CatalogLookupStatus.Unavailable)
                : new CatalogLookup(CatalogLookupStatus.Found, subject);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Convert network/time-out failures into a controlled API response and log for diagnosis.
            logger.LogWarning(ex, "Catalog lookup failed for subject {SubjectId}", id);
            return new CatalogLookup(CatalogLookupStatus.Unavailable);
        }
    }
}

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, ExamOrder> _orders = new();
    private int _nextId;
    public IReadOnlyCollection<ExamOrder> GetAll() => _orders.Values.OrderByDescending(o => o.Id).ToArray();
    public ExamOrder? Get(int id) => _orders.GetValueOrDefault(id);
    public ExamOrder Add(CreateOrderRequest request, string subjectName)
    {
        var id = Interlocked.Increment(ref _nextId);
        var order = new ExamOrder(id, request.SubjectId, subjectName, request.CandidateName.Trim(),
            request.ExamSession.Trim(), DateTimeOffset.UtcNow);
        _orders[id] = order;
        return order;
    }
}
