using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
if (Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");

// AuthService owns identity data, refresh-token records, and the mapping from roles to permissions.
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Auth")
        ?? throw new InvalidOperationException("ConnectionStrings:Auth is required.")));

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    // Password rules are intentionally visible to learners; production also needs MFA and lockout tuning.
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AuthDbContext>()
.AddSignInManager();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    // The gateway uses the same validation rules so invalid/expired tokens are rejected at the edge.
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
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("permission", "users.manage"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});


app.UseAuthentication();
app.UseAuthorization();

// Workshop shortcut: create the schema automatically. Production deployments should use EF migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var attempts = 0;
    while (true)
    {
        try { await db.Database.EnsureCreatedAsync(); break; }
        catch when (++attempts < 40) { await Task.Delay(TimeSpan.FromSeconds(3)); }
    }

    // A role is a named bundle of permissions. Policies check permission claims, not hard-coded role names.
    var rolePermissions = new Dictionary<string, string[]>
    {
        ["Candidate"] = ["catalog.read", "registrations.create", "profile.read"],
        ["Examiner"] = ["catalog.read", "catalog.manage", "registrations.read", "registrations.create", "profile.read"],
        ["Administrator"] = ["catalog.read", "catalog.manage", "registrations.read", "registrations.create", "profile.read", "users.manage"]
    };

    foreach (var (roleName, permissions) in rolePermissions)
    {
        var role = await roles.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new IdentityRole(roleName);
            await roles.CreateAsync(role);
        }

        // Keep role claims aligned with the permission matrix whenever the service starts.
        var existing = await roles.GetClaimsAsync(role);
        foreach (var claim in existing.Where(c => c.Type == "permission"))
            await roles.RemoveClaimAsync(role, claim);
        foreach (var permission in permissions)
            await roles.AddClaimAsync(role, new Claim("permission", permission));
    }

    // The first administrator is created only from deployment environment variables, never public self-registration.
    var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
    var adminPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
        && await users.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var created = await users.CreateAsync(admin, adminPassword);
        if (created.Succeeded) await users.AddToRoleAsync(admin, "Administrator");
        else app.Logger.LogError("Bootstrap administrator creation failed: {Errors}",
            string.Join("; ", created.Errors.Select(e => e.Description)));
    }
}

app.MapGet("/health", () => Results.Ok(new { service = "auth", status = "healthy" }));

// Public registration always assigns Candidate; accepting a role from this request would allow privilege escalation.
app.MapPost("/api/auth/register", async (RegisterRequest request, UserManager<IdentityUser> users) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Email and password are required." });

    var email = request.Email.Trim();
    var user = new IdentityUser { UserName = email, Email = email };
    var result = await users.CreateAsync(user, request.Password);
    if (!result.Succeeded)
        return Results.ValidationProblem(result.Errors.GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

    await users.AddToRoleAsync(user, "Candidate");
    return Results.Created($"/api/auth/users/{user.Id}", new { user.Id, user.Email, role = "Candidate" });
});

// Login checks both password and account lockout, then issues a short-lived access token and a rotating refresh token.
app.MapPost("/api/auth/login", async (LoginRequest request, UserManager<IdentityUser> users,
    SignInManager<IdentityUser> signIn, AuthDbContext db, RoleManager<IdentityRole> roleManager) =>
{
    var user = await users.FindByEmailAsync(request.Email.Trim());
    if (user is null || !(await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded)
        return Results.Unauthorized();

    return Results.Ok(await IssueTokenPairAsync(user, users, roleManager, db, jwt, signingKey));
});

// Refresh tokens are opaque random values. Only their SHA-256 hashes are persisted in the database.
app.MapPost("/api/auth/refresh", async (RefreshRequest request, AuthDbContext db,
    UserManager<IdentityUser> users, RoleManager<IdentityRole> roleManager) =>
{
    try
    {
        // Keep lookup, revocation, and replacement inside one database transaction to prevent parallel token reuse.
        await using var transaction = await db.Database.BeginTransactionAsync();
    var tokenHash = HashToken(request.RefreshToken);
    var stored = await db.RefreshTokens.Include(x => x.User)
        .SingleOrDefaultAsync(x => x.TokenHash == tokenHash);

    if (stored is null || stored.RevokedAtUtc is not null || stored.ExpiresAtUtc <= DateTime.UtcNow
        || (stored.User.LockoutEnabled && stored.User.LockoutEnd is not null
            && stored.User.LockoutEnd > DateTimeOffset.UtcNow))
        return Results.Unauthorized();

    // Rotate atomically: revoke the old token before issuing a replacement.
    stored.RevokedAtUtc = DateTime.UtcNow;
    var pair = await IssueTokenPairAsync(stored.User, users, roleManager, db, jwt, signingKey);
    stored.ReplacedByTokenHash = HashToken(pair.RefreshToken);
    await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Results.Ok(pair);
    }
    catch (DbUpdateConcurrencyException)
    {
        // Another request already rotated this token; treat the replay as an invalid session.
        return Results.Unauthorized();
    }
}).AddEndpointFilter(async (context, next) =>
{
    // The request DTO is checked here to reject empty refresh-token strings consistently.
    var request = context.GetArgument<RefreshRequest>(0);
    return string.IsNullOrWhiteSpace(request.RefreshToken)
        ? Results.BadRequest(new { message = "Refresh token is required." })
        : await next(context);
});

// Logout revokes the presented refresh token; an already-issued access token remains valid until its short expiry.
app.MapPost("/api/auth/logout", async (RefreshRequest request, AuthDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
        return Results.BadRequest(new { message = "Refresh token is required." });

    // Calculate the hash before the EF query; local functions cannot be translated inside expression trees.
    var tokenHash = HashToken(request.RefreshToken);
    var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash);
    if (stored is not null && stored.RevokedAtUtc is null)
    {
        stored.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
});

// The /me endpoint demonstrates reading identity, roles, and fine-grained permissions from JWT claims.
// app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new
// {
//     id = user.FindFirstValue(JwtRegisteredClaimNames.Sub),
//     email = user.FindFirstValue(JwtRegisteredClaimNames.Email),
//     roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value),
//     permissions = user.FindAll("permission").Select(c => c.Value)
// })).RequireAuthorization();

app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new
{
    id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub),
    email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue(JwtRegisteredClaimNames.Email),
    roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value),
    permissions = user.FindAll("permission").Select(c => c.Value)
})).RequireAuthorization();

// Admin user management: list accounts without password/security fields.
app.MapGet("/api/auth/users", async (UserManager<IdentityUser> users) =>
{
    var result = new List<UserSummary>();
    foreach (var user in users.Users.OrderBy(x => x.Email))
    {
        var roles = await users.GetRolesAsync(user);
        result.Add(new UserSummary(user.Id, user.Email ?? "", roles,
            user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow));
    }
    return Results.Ok(result);
}).RequireAuthorization("AdminOnly");

// Provision staff accounts. Public registration cannot create Examiner or Administrator accounts.
app.MapPost("/api/auth/users", async (CreateStaffRequest request, UserManager<IdentityUser> users) =>
{
    if (request.Role is not ("Examiner" or "Administrator"))
        return Results.BadRequest(new { message = "Only Examiner or Administrator can be provisioned here." });

    var email = request.Email.Trim();
    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
    var created = await users.CreateAsync(user, request.TemporaryPassword);
    if (!created.Succeeded)
        return Results.ValidationProblem(created.Errors.GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

    await users.AddToRoleAsync(user, request.Role);
    return Results.Created($"/api/auth/users/{user.Id}", new { user.Id, user.Email, role = request.Role });
}).RequireAuthorization("AdminOnly");

// Role changes update Identity. Existing access tokens keep their old claims until they expire; refresh to receive new claims.
app.MapPut("/api/auth/users/{userId}/role", async (string userId, ChangeRoleRequest request,
    ClaimsPrincipal actor, UserManager<IdentityUser> users) =>
{
    if (request.Role is not ("Candidate" or "Examiner" or "Administrator"))
        return Results.BadRequest(new { message = "Role must be Candidate, Examiner, or Administrator." });

    var target = await users.FindByIdAsync(userId);
    if (target is null) return Results.NotFound();
    var actorId = actor.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (target.Id == actorId && request.Role != "Administrator")
        return Results.BadRequest(new { message = "Administrators cannot remove their own Administrator role." });

    var currentRoles = await users.GetRolesAsync(target);
    var result = await users.RemoveFromRolesAsync(target, currentRoles);
    if (!result.Succeeded) return Results.BadRequest(result.Errors);
    result = await users.AddToRoleAsync(target, request.Role);
    if (!result.Succeeded) return Results.BadRequest(result.Errors);
    await users.UpdateSecurityStampAsync(target);
    return Results.Ok(new { target.Id, target.Email, role = request.Role });
}).RequireAuthorization("AdminOnly");

// Disable/enable a user using Identity lockout. This does not delete audit history or registration records.
app.MapPut("/api/auth/users/{userId}/status", async (string userId, ChangeStatusRequest request,
    ClaimsPrincipal actor, UserManager<IdentityUser> users) =>
{
    var target = await users.FindByIdAsync(userId);
    if (target is null) return Results.NotFound();
    if (target.Id == actor.FindFirstValue(JwtRegisteredClaimNames.Sub) && !request.Enabled)
        return Results.BadRequest(new { message = "Administrators cannot disable their own account." });

    target.LockoutEnabled = true;
    target.LockoutEnd = request.Enabled ? null : DateTimeOffset.MaxValue;
    var result = await users.UpdateAsync(target);
    if (!result.Succeeded) return Results.BadRequest(result.Errors);
    await users.UpdateSecurityStampAsync(target);
    return Results.Ok(new { target.Id, target.Email, enabled = request.Enabled });
}).RequireAuthorization("AdminOnly");

app.Run();

// Shared token-pair builder keeps login and refresh responses consistent and adds current role permission claims.
static async Task<TokenPairResponse> IssueTokenPairAsync(IdentityUser user, UserManager<IdentityUser> users,
    RoleManager<IdentityRole> roleManager, AuthDbContext db, IConfiguration jwt, string signingKey)
{
    var roles = await users.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new(ClaimTypes.Name, user.Email ?? user.UserName ?? user.Id),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
    foreach (var roleName in roles)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null) continue;
        var roleClaims = await roleManager.GetClaimsAsync(role);
        claims.AddRange(roleClaims.Where(c => c.Type == "permission"));
    }

    var now = DateTime.UtcNow;
    var accessMinutes = int.TryParse(jwt["AccessTokenMinutes"], out var configuredMinutes) ? configuredMinutes : 10;
    var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
        now, now.AddMinutes(accessMinutes), credentials);

    // A 64-byte random value is unguessable; only its hash is saved so a database leak does not expose sessions.
    var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    var refreshDays = int.TryParse(jwt["RefreshTokenDays"], out var configuredDays) ? configuredDays : 7;
    db.RefreshTokens.Add(new RefreshToken
    {
        UserId = user.Id,
        TokenHash = HashToken(rawRefreshToken),
        CreatedAtUtc = now,
        ExpiresAtUtc = now.AddDays(refreshDays)
    });
    await db.SaveChangesAsync();

    return new TokenPairResponse(new JwtSecurityTokenHandler().WriteToken(token), rawRefreshToken,
        "Bearer", token.ValidTo, user.Id, user.Email ?? "", roles, claims.Where(c => c.Type == "permission").Select(c => c.Value).Distinct());
}

static string HashToken(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record CreateStaffRequest(string Email, string TemporaryPassword, string Role);
public sealed record ChangeRoleRequest(string Role);
public sealed record ChangeStatusRequest(bool Enabled);
public sealed record UserSummary(string Id, string Email, IList<string> Roles, bool Disabled);
public sealed record TokenPairResponse(string AccessToken, string RefreshToken, string TokenType,
    DateTime ExpiresAtUtc, string UserId, string Email, IList<string> Roles, IEnumerable<string> Permissions);

// Identity tables and refresh-token records live in the Auth database, separate from examination data.
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        // SQL Server rowversion detects two refresh requests trying to rotate the same token concurrently.
        builder.Entity<RefreshToken>().Property(x => x.RowVersion).IsRowVersion();
        builder.Entity<RefreshToken>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public IdentityUser User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
