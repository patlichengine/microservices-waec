using Microsoft.EntityFrameworkCore;
using StudentService.Data;
using StudentService.Messaging;
using StudentService.Repositories;
using StudentService.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});


builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Students"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService.Services.StudentService>();
builder.Services.AddScoped<IStudentEventPublisher, StudentEventPublisher>();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
    db.Database.Migrate();
}

app.UseSwagger(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { service = "student", status = "healthy" }));
app.MapControllers();
app.Run();
