using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
if (Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");

// 1. Gateway's own Swagger (for /health or any local gateway endpoints)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
});

// 2. Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt["Issuer"],
        ValidateAudience = true, ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogRead", policy => policy.RequireClaim("permission", "catalog.read"));
    options.AddPolicy("CatalogManage", policy => policy.RequireClaim("permission", "catalog.manage"));
    options.AddPolicy("RegistrationsRead", policy => policy.RequireClaim("permission", "registrations.read"));
    options.AddPolicy("RegistrationsCreate", policy => policy.RequireClaim("permission", "registrations.create"));
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 3. Intercept downstream /swagger/*/swagger/v1/swagger.json responses and inject JWT locks
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/swagger/", StringComparison.OrdinalIgnoreCase) &&
        path.EndsWith("/swagger.json", StringComparison.OrdinalIgnoreCase) &&
        !path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next();

        buffer.Position = 0;
        var json = await new StreamReader(buffer).ReadToEndAsync();

        if (context.Response.StatusCode == 200 && !string.IsNullOrWhiteSpace(json))
        {
            json = InjectJwtSecurityWhereAuthorized(json, app.Configuration);
            context.Response.Headers.ContentLength = null; // Reset length after modifying JSON
        }

        context.Response.Body = originalBody;
        await context.Response.WriteAsync(json);
        return;
    }

    await next();
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/auth/swagger/v1/swagger.json", "Auth Service v1");
    options.SwaggerEndpoint("/swagger/catalog/swagger/v1/swagger.json", "Catalog Service v1");
    options.SwaggerEndpoint("/swagger/registration/swagger/v1/swagger.json", "Registrations Service v1");
    options.SwaggerEndpoint("/swagger/orders/swagger/v1/swagger.json", "Orders Service v1");
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    options.EnablePersistAuthorization(); // Keeps your JWT logged in across all 5 dropdown services
});

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { service = "gateway", status = "healthy" }));
app.MapReverseProxy();
app.Run();



static string InjectJwtSecurityWhereAuthorized(string swaggerJson, IConfiguration config)
{
    var doc = JsonNode.Parse(swaggerJson)?.AsObject();
    if (doc is null) return swaggerJson;

    // 1. Ensure the "Bearer" security scheme exists on this service's Swagger doc
    var components = doc["components"]?.AsObject() ?? new JsonObject();
    var schemes = components["securitySchemes"]?.AsObject() ?? new JsonObject();
    schemes["Bearer"] = JsonNode.Parse("""
    {
      "type": "http",
      "scheme": "bearer",
      "bearerFormat": "JWT",
      "description": "Enter your JWT token."
    }
    """);
    components["securitySchemes"] = schemes;
    doc["components"] = components;

    // 2. Collect protected route prefixes & HTTP methods from YARP config
    var protectedRoutes = config.GetSection("ReverseProxy:Routes").GetChildren()
        .Select(r => new
        {
            Policy = r["AuthorizationPolicy"],
            Path = r["Match:Path"]?.Replace("{**catch-all}", "").TrimEnd('/'),
            Methods = r.GetSection("Match:Methods").Get<string[]>()
        })
        .Where(r => !string.IsNullOrWhiteSpace(r.Policy) &&
                    !r.Policy.Equals("anonymous", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(r.Path))
        .ToList();

    // 3. Attach the lock ONLY to matching paths/methods (or keep existing locks from downstream [Authorize])
    if (doc["paths"] is JsonObject paths)
    {
        foreach (var (routePath, pathItemNode) in paths)
        {
            if (pathItemNode is not JsonObject pathItem) continue;

            foreach (var (method, operationNode) in pathItem)
            {
                if (operationNode is not JsonObject operation) continue;

                // Skip if downstream service already marked this endpoint with security
                if (operation["security"] is JsonArray existing && existing.Count > 0) continue;

                var isProtected = protectedRoutes.Any(r =>
                    routePath.StartsWith(r.Path!, StringComparison.OrdinalIgnoreCase) &&
                    (r.Methods is null || r.Methods.Length == 0 ||
                     r.Methods.Contains(method, StringComparer.OrdinalIgnoreCase)));

                if (isProtected)
                {
                    operation["security"] = JsonNode.Parse("""[{ "Bearer": [] }]""");
                }
            }
        }
    }

    return doc.ToJsonString();
}

