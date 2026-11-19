using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Http.Features; // Add this using at the top of Gateway Program.cs


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

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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
// 3. Intercept downstream /swagger/*/swagger/v1/swagger.json responses and inject JWT locks
// 3. Intercept downstream /swagger/*/swagger/v1/swagger.json responses and inject JWT locks
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/swagger/", StringComparison.OrdinalIgnoreCase) &&
        path.EndsWith("/swagger.json", StringComparison.OrdinalIgnoreCase) &&
        !path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Headers.Remove("Accept-Encoding");

        var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>()!;
        using var buffer = new MemoryStream();
        var bufferFeature = new StreamResponseBodyFeature(buffer);
        context.Features.Set<IHttpResponseBodyFeature>(bufferFeature);

        try
        {
            await next();
        }
        finally
        {
            context.Features.Set(originalBodyFeature);
        }

        buffer.Position = 0;
        var json = await new StreamReader(buffer).ReadToEndAsync();

        if (context.Response.StatusCode == 200 && !string.IsNullOrWhiteSpace(json))
        {
            json = InjectJwtSecurityWhereAuthorized(json, app.Configuration);
        }
        else
        {
            // Return a valid minimal OpenAPI 3.0 stub showing the exact HTTP error in Swagger UI
            json = $$"""
            {
              "openapi": "3.0.4",
              "info": {
                "title": "Downstream Error (HTTP {{context.Response.StatusCode}}) for {{path}}",
                "version": "v1"
              },
              "paths": {}
            }
            """;
            context.Response.StatusCode = 200;
        }

        context.Response.Headers.ContentLength = null;
        context.Response.Headers.Remove("Transfer-Encoding");
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(json);
        return;
    }

    await next();
});


// Explicitly restrict Gateway's own Swagger JSON to /swagger/v1/swagger.json
app.UseSwagger(options =>
{
    //options.RouteTemplate = "swagger/{documentName}/swagger.json";
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});


app.UseSwaggerUI(options =>
{
    // Put Gateway API v1 first so Swagger UI always loads cleanly even if a downstream service is stopped
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    options.SwaggerEndpoint("/swagger/auth/swagger/v1/swagger.json", "Auth Service v1");
    options.SwaggerEndpoint("/swagger/catalog/swagger/v1/swagger.json", "Catalog Service v1");
    options.SwaggerEndpoint("/swagger/registration/swagger/v1/swagger.json", "Registrations Service v1");
    options.SwaggerEndpoint("/swagger/orders/swagger/v1/swagger.json", "Orders Service v1");
    options.SwaggerEndpoint("/swagger/student/swagger/v1/swagger.json", "Student Service v1");
    options.EnablePersistAuthorization();
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

