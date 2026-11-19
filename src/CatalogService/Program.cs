using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
// Register a singleton store. In-memory storage keeps early labs simple; production needs a database.
builder.Services.AddSingleton<SubjectStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});


app.MapGet("/health", () => Results.Ok(new { service = "catalog", status = "healthy" }));
app.MapGet("/api/catalog/subjects", (SubjectStore store) => Results.Ok(store.GetAll()));
app.MapGet("/api/catalog/subjects/{id:int}", (int id, SubjectStore store) =>
{
    var subject = store.Get(id);
    return subject is null ? Results.NotFound() : Results.Ok(subject);
});
app.MapPost("/api/catalog/subjects", (CreateSubjectRequest request, SubjectStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Code and name are required." });
    var subject = store.Add(request);
    return Results.Created($"/api/catalog/subjects/{subject.Id}", subject);
});
app.MapPut("/api/catalog/subjects/{id:int}", (int id, UpdateSubjectRequest request, SubjectStore store) =>
{
    var updated = store.Update(id, request);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});
app.MapDelete("/api/catalog/subjects/{id:int}", (int id, SubjectStore store) =>
    store.Delete(id) ? Results.NoContent() : Results.NotFound());
app.Run();

// Request records are API contracts, separate from the stored entity.
public sealed record CreateSubjectRequest(string Code, string Name, bool Active = true);
public sealed record UpdateSubjectRequest(string Code, string Name, bool Active);
public sealed record Subject(int Id, string Code, string Name, bool Active);

// Thread-safe in-memory repository for this training version.
public sealed class SubjectStore
{
    private readonly ConcurrentDictionary<int, Subject> _subjects = new();
    private int _nextId = 2;
    public SubjectStore()
    {
        _subjects[1] = new Subject(1, "ENG", "English Language", true);
        _subjects[2] = new Subject(2, "MAT", "Mathematics", true);
    }
    public IReadOnlyCollection<Subject> GetAll() => _subjects.Values.OrderBy(s => s.Id).ToArray();
    public Subject? Get(int id) => _subjects.GetValueOrDefault(id);
    public Subject Add(CreateSubjectRequest request)
    {
        var id = Interlocked.Increment(ref _nextId);
        var item = new Subject(id, request.Code.Trim().ToUpperInvariant(), request.Name.Trim(), request.Active);
        _subjects[id] = item;
        return item;
    }
    public Subject? Update(int id, UpdateSubjectRequest request)
    {
        if (!_subjects.ContainsKey(id)) return null;
        var item = new Subject(id, request.Code.Trim().ToUpperInvariant(), request.Name.Trim(), request.Active);
        _subjects[id] = item;
        return item;
    }
    public bool Delete(int id) => _subjects.TryRemove(id, out _);
}
