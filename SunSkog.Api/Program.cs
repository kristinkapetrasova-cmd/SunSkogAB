using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Routing; // kvůli /dev/routes2
using Microsoft.AspNetCore.Http;    // kvůli StatusCodes
using Microsoft.AspNetCore.Mvc;
using System.Linq;                  // kvůli Any(), Select(), atd.
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Endpoints;
using SunSkog.Api.Seeds;
using SunSkog.Api.Storage.Entities; // kvůli Team, TeamMembership apod.

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SunSkog API", Version = "v1" });

    // JWT schema
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Zadej JWT jako: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- CORS: DEV režim – povolíme vše z FE ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()   // dovolíme jakýkoliv origin (localhost:5173 apod.)
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

// --- DB připojení ---
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
    }));

// --- Identity (ApplicationUser) ---
builder.Services
    .AddIdentityCore<ApplicationUser>(o =>
    {
        o.Password.RequireDigit = true;
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- JWT ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    // Dev fallback – v produkci nutně nastav v konfiguraci!
    jwtKey = "dev-only-secret-change-me-please-change-me-dev-only!";
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// (Volitelné) Issuer/Audience
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero,

            // DŮLEŽITÉ:
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

// --- Authorization / Policies ---
builder.Services.AddAuthorization(options =>
{
    // workflow výkazů (submit/approve/return)
    options.AddPolicy("CanApproveTimesheet", policy =>
        policy.RequireRole("TeamLead", "Management", "Admin"));

    // export výkazů (účtárna + vedení + admin)
    options.AddPolicy("CanExportTimesheets", policy =>
        policy.RequireRole("Accountant", "Management", "Admin"));

    // sklad (QR, příjem/výdej, pohyby)
    options.AddPolicy("CanUseInventory", policy =>
        policy.RequireRole("Warehouse", "Management", "Admin"));
});

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // V DEV přidej do odpovědi detail výjimky (jen pro ladění)
        if (builder.Environment.IsDevelopment() && ctx.Exception is Exception ex)
        {
            ctx.ProblemDetails.Extensions["exception"]  = ex.GetType().FullName;
            ctx.ProblemDetails.Extensions["message"]    = ex.Message;
            ctx.ProblemDetails.Extensions["stackTrace"] = ex.StackTrace;
        }
    };
});


// Configure JSON to use UTF-8 encoding
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});
builder.Services.AddRouting(o => o.LowercaseUrls = true);

var app = builder.Build();

// Swagger v production pro testování
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseExceptionHandler();      // 5xx -> ProblemDetails
app.UseStatusCodePages();       // 4xx -> text/ProblemDetails podle akceptu
app.UseRouting();

// CORS – použijeme default policy výše (AllowAnyOrigin/AnyHeader/AnyMethod)
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// --- migrace + seed ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    
    try
    {
        app.Logger.LogInformation("Ensuring database exists...");
        
        // Vytvoř databázi pokud neexistuje
        await db.Database.EnsureCreatedAsync();
        
        app.Logger.LogInformation("Database ready.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database setup failed.");
    }

    try
    {
        await IdentitySeeder.SeedAsync(app.Services, app.Logger);
        app.Logger.LogInformation("Identity seed finished.");

        await AppDataSeeder.SeedAsync(app.Services, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Identity seeding failed.");
    }
}

// --- Auth: login (rozšířený output + role v tokenu) ---
app.MapPost("/auth/login", async (LoginRequest req,
                                  UserManager<ApplicationUser> users,
                                  IConfiguration cfg) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email a heslo jsou povinné." });

    var user = await users.FindByEmailAsync(req.Email);
    if (user == null)
        return Results.Unauthorized();

    if (!await users.CheckPasswordAsync(user, req.Password))
        return Results.Unauthorized();

    // načti role
    var roles = await users.GetRolesAsync(user);

    // --- JWT ---
    var key = cfg["Jwt:Key"] ?? jwtKey!;
    var keyBytes = Encoding.UTF8.GetBytes(key);
    var signingKeyLocal = new SymmetricSecurityKey(keyBytes);
    var credentials = new SigningCredentials(signingKeyLocal, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
    };

    // přidej role jako standardní "role" claimy
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var issuer = cfg["Jwt:Issuer"];
    var audience = cfg["Jwt:Audience"];

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: credentials
    );

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    // vrátíme i základní info o uživateli a role
    return Results.Ok(new
    {
        token = jwt,
        user = new
        {
            id = user.Id,
            email = user.Email,
            name = user.FullName ?? user.UserName ?? user.Email,
            roles = roles
        }
    });
})
.AllowAnonymous()
.WithTags("Auth")
.WithOpenApi();

// --- Kdo jsem (z tokenu) ---
app.MapGet("/auth/me", [Authorize] async (ClaimsPrincipal me, UserManager<ApplicationUser> users) =>
{
    var uid = me.FindFirstValue(ClaimTypes.NameIdentifier);
    var u = uid != null ? await users.FindByIdAsync(uid) : null;
    var roles = u != null ? await users.GetRolesAsync(u) : new List<string>();

    return Results.Ok(new
    {
        id = uid,
        email = me.FindFirstValue(ClaimTypes.Email),
        name = me.Identity?.Name,
        roles
    });
})
.RequireAuthorization()
.WithTags("Auth")
.WithOpenApi();

app.MapGet("/dev/identity-check", async (UserManager<ApplicationUser> users) =>
{
    var u1 = await users.FindByEmailAsync("admin@sunskog.local");
    var u2 = await users.FindByEmailAsync("user@sunskog.local");

    return Results.Ok(new {
        adminFound = u1 != null,
        userFound  = u2 != null,
        adminNorm  = u1?.NormalizedEmail,
        userNorm   = u2?.NormalizedEmail
    });
})
.AllowAnonymous()
.WithTags("Dev")
.WithOpenApi();

// --- Ping ---
app.MapGet("/api/ping", () => Results.Ok(new { message = "pong" }))
   .WithTags("System")
   .WithOpenApi();

// --- Chráněný endpoint ---
app.MapGet("/api/secret", [Authorize] () => Results.Ok(new { value = "🔒 very secret data" }))
   .RequireAuthorization()
   .WithTags("System")
   .WithOpenApi();

// --- API endpoints (zapnuté) ---
app.MapTimesheetEndpoints();
app.MapTimesheetEntryEndpoints();
app.MapRateEndpoints();
app.MapTimesheetWorkflowEndpoints();
app.MapAdminTimesheetCsvExportEndpoints();
app.MapAdminReportsEndpoints();
app.MapInventoryEndpoints();
app.MapInventoryQrEndpoints();
app.MapTimesheetDevEndpoints();
//app.MapTeamDevEndpoints();
app.MapAdminTeamEndpoints();
//app.MapTeamEndpoints();
app.MapAssignmentEndpoints();
app.MapCategoryEndpoints();

// Admin timesheet endpoints (používá statickou metodu Map místo extension)
AdminTimesheetEndpoints.Map(app);
AdminTimesheetDetailEndpoints.Map(app);
AdminDashboardEndpoints.Map(app);

// --- Dev: diagnostické endpointy ---
app.MapGet("/dev/where", () =>
{
    return Results.Ok(new
    {
        baseDir = AppContext.BaseDirectory,
        procId = Environment.ProcessId,
        urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    });
})
.WithTags("Dev")
.AllowAnonymous();

app.MapGet("/dev/ping", () => Results.Ok(new { ok = true, at = DateTime.UtcNow }))
.WithTags("Dev")
.AllowAnonymous();

app.MapGet("/dev/routes2", (EndpointDataSource eds) =>
{
    var routes = eds.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => new
        {
            route = e.RoutePattern.RawText,
            methods = string.Join("|", e.Metadata
                .OfType<HttpMethodMetadata>()
                .SelectMany(m => m.HttpMethods))
        })
        .OrderBy(x => x.route)
        .ToList();

    return Results.Ok(routes);
})
.WithTags("Dev")
.AllowAnonymous();

app.MapGet("/dev/me", (ClaimsPrincipal user) => new
{
    name = user.Identity?.Name,
    roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
})
.RequireAuthorization()
.WithTags("Dev")
.WithOpenApi();

app.MapGet("/dev/db-check", async (ApplicationDbContext db) =>
{
    try
    {
        var can = await db.Database.CanConnectAsync();
        var provider = db.Database.ProviderName ?? "(unknown)";
        int users = 0;
        try { users = await db.Users.CountAsync(); } catch { }
        return Results.Ok(new { canConnect = can, provider, usersCount = users });
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "DB check failed", detail: ex.Message, statusCode: 500);
    }
})
.WithTags("Dev")
.WithOpenApi();

// --- Dev: reset admin + test user ---
app.MapPost("/dev/reset-admin", [AllowAnonymous] async (
    ILogger<Program> logger,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles) =>
{
    const string email = "admin@sunskog.local";
    const string password = "Admin123!";
    const string roleName = "Admin";

    try
    {
        if (!await roles.RoleExistsAsync(roleName))
        {
            var roleRes = await roles.CreateAsync(new IdentityRole(roleName));
            if (!roleRes.Succeeded)
            {
                var err = string.Join("; ", roleRes.Errors.Select(e => e.Description));
                logger.LogError("Creating role '{Role}' failed: {Err}", roleName, err);
                return Results.Problem(title: "Role create failed", detail: err, statusCode: 500);
            }
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "System Administrator"
            };

            var createRes = await users.CreateAsync(user, password);
            if (!createRes.Succeeded)
            {
                var err = string.Join("; ", createRes.Errors.Select(e => e.Description));
                logger.LogError("Creating admin user failed: {Err}", err);
                return Results.Problem(title: "User create failed", detail: err, statusCode: 500);
            }
        }
        else
        {
            if (await users.HasPasswordAsync(user))
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var resetRes = await users.ResetPasswordAsync(user, token, password);
                if (!resetRes.Succeeded)
                {
                    var err = string.Join("; ", resetRes.Errors.Select(e => e.Description));
                    logger.LogError("Reset admin password failed: {Err}", err);
                    return Results.Problem(title: "Password reset failed", detail: err, statusCode: 500);
                }
            }
            else
            {
                var addRes = await users.AddPasswordAsync(user, password);
                if (!addRes.Succeeded)
                {
                    var err = string.Join("; ", addRes.Errors.Select(e => e.Description));
                    logger.LogError("Add admin password failed: {Err}", err);
                    return Results.Problem(title: "Add password failed", detail: err, statusCode: 500);
                }
            }

            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.EmailConfirmed = true;
            await users.UpdateAsync(user);
        }

        if (!await users.IsInRoleAsync(user, roleName))
        {
            var roleAddRes = await users.AddToRoleAsync(user, roleName);
            if (!roleAddRes.Succeeded)
            {
                var err = string.Join("; ", roleAddRes.Errors.Select(e => e.Description));
                logger.LogError("Add role to admin failed: {Err}", err);
                return Results.Problem(title: "Add role failed", detail: err, statusCode: 500);
            }
        }

        logger.LogInformation("DEV reset-admin OK for {Email}", email);
        return Results.Ok(new { email, password, role = roleName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DEV reset-admin failed (exception)");
        return Results.Problem(title: "reset-admin failed", detail: ex.Message, statusCode: 500);
    }
})
.WithTags("Dev")
.WithOpenApi();

app.MapPost("/dev/create-test-user", [AllowAnonymous] async (
    ILogger<Program> logger,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles) =>
{
    const string email = "user@sunskog.local";
    const string password = "User123$";
    const string roleName = "User";

    try
    {
        if (!await roles.RoleExistsAsync(roleName))
        {
            var roleRes = await roles.CreateAsync(new IdentityRole(roleName));
            if (!roleRes.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", roleRes.Errors.Select(e => e.Description)) });
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Test User"
            };

            var createRes = await users.CreateAsync(user, password);
            if (!createRes.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", createRes.Errors.Select(e => e.Description)) });
        }
        else
        {
            if (await users.HasPasswordAsync(user))
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var resetRes = await users.ResetPasswordAsync(user, token, password);
                if (!resetRes.Succeeded)
                    return Results.BadRequest(new { error = string.Join("; ", resetRes.Errors.Select(e => e.Description)) });
            }
            else
            {
                var addRes = await users.AddPasswordAsync(user, password);
                if (!addRes.Succeeded)
                    return Results.BadRequest(new { error = string.Join("; ", addRes.Errors.Select(e => e.Description)) });
            }

            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.EmailConfirmed = true;
            await users.UpdateAsync(user);
        }

        if (!await users.IsInRoleAsync(user, roleName))
        {
            var roleAddRes = await users.AddToRoleAsync(user, roleName);
            if (!roleAddRes.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", roleAddRes.Errors.Select(e => e.Description)) });
        }

        logger.LogInformation("DEV create-test-user ok for {Email}", email);
        return Results.Ok(new { email, password, role = roleName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DEV create-test-user failed");
        return Results.Problem(title: "create-test-user failed", detail: ex.Message, statusCode: 500);
    }
})
.WithTags("Dev")
.WithOpenApi();

// --- Dev: rychlý listing timesheetů ---
app.MapGet("/dev/timesheets", async (ApplicationDbContext db) =>
    await db.Timesheets
        .AsNoTracking()
        .OrderByDescending(t => t.PeriodStart)
        .Select(t => new
        {
            t.Id,
            t.EmployeeId,
            t.PeriodStart,
            t.PeriodEnd,
            Status = t.Status.ToString(),
            t.TotalHours,
            t.TotalKm,
            t.TotalPieces,
            t.TotalPay
        })
        .ToListAsync()
)
.WithTags("Dev")
.WithOpenApi();

// --- Admin: vytvoření nového uživatele (Identity) ---
app.MapPost("/api/users", [Authorize(Roles = "Admin")] async (
    [FromBody] CreateUserRequest req,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    ILogger<Program> logger
) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email a heslo jsou povinné." });

    var existing = await users.FindByEmailAsync(req.Email);
    if (existing != null)
        return Results.Conflict(new { error = "Uživatel s tímto emailem již existuje." });

    var user = new ApplicationUser
    {
        UserName = req.Email,
        Email = req.Email,
        EmailConfirmed = true,
        FullName = req.Name ?? req.Email
    };

    var createRes = await users.CreateAsync(user, req.Password);
    if (!createRes.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", createRes.Errors.Select(e => e.Description)) });

    if (req.Roles != null && req.Roles.Any())
    {
        foreach (var r in req.Roles)
        {
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new IdentityRole(r));

            await users.AddToRoleAsync(user, r);
        }
    }

    logger.LogInformation("Admin created user {Email} ({Roles})", req.Email, string.Join(",", req.Roles ?? Array.Empty<string>()));

    return Results.Ok(new
    {
        email = user.Email,
        roles = req.Roles,
        id = user.Id
    });
})
.WithTags("Admin")
.WithOpenApi();

// --- Admin: seznam uživatelů (Identity + tým) ---
app.MapGet("/api/users", [Authorize(Roles = "Admin")] async (
    UserManager<ApplicationUser> users,
    ApplicationDbContext db
) =>
{
    var allUsers = await users.Users
        .OrderBy(u => u.Email)
        .ToListAsync();

    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    var memberships = await db.TeamMemberships
        .Include(tm => tm.Team)
        .Where(tm => tm.FromDate <= today && (tm.ToDate == null || tm.ToDate >= today))
        .ToListAsync();

    var result = new List<object>(allUsers.Count);

    foreach (var u in allUsers)
    {
        var roles = await users.GetRolesAsync(u);
        var isLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

        var tm = memberships
            .Where(m => m.UserId == u.Id)
            .OrderByDescending(m => m.FromDate)
            .FirstOrDefault();

        result.Add(new
        {
            id = u.Id,
            email = u.Email,
            name = u.FullName ?? u.UserName ?? u.Email,
            roles,
            isLockedOut,
            teamId = tm?.TeamId,
            teamName = tm?.Team?.Name,
            teamRole = tm?.Role
        });
    }

    return Results.Ok(result);
})
.WithTags("Admin")
.WithOpenApi();

// --- Employees: jednoduchý seznam pro přiřazování položek ---
// Přístupné pro Management, Warehouse, TeamLead, Admin
app.MapGet("/api/employees", [Authorize] async (
    UserManager<ApplicationUser> users,
    ClaimsPrincipal currentUser
) =>
{
    // Kontrola oprávnění - musí mít jednu z těchto rolí
    var allowedRoles = new[] { "Admin", "Management", "Warehouse", "TeamLead" };
    var hasAccess = allowedRoles.Any(role => currentUser.IsInRole(role));
    
    if (!hasAccess)
    {
        return Results.Forbid();
    }
    
    var allUsers = await users.Users
        .OrderBy(u => u.FullName ?? u.UserName ?? u.Email)
        .Select(u => new
        {
            id = u.Id,
            name = u.FullName ?? u.UserName ?? u.Email,
            email = u.Email
        })
        .ToListAsync();

    return Results.Ok(allUsers);
})
.WithTags("Employees")
.WithOpenApi();

// --- My Team: pro TeamLeada - vrátí jeho tým s členy ---
app.MapGet("/api/my-team", [Authorize] async (
    ClaimsPrincipal currentUser,
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager
) =>
{
    var userId = userManager.GetUserId(currentUser);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var today = DateOnly.FromDateTime(DateTime.Today);

    // Najít tým kde je uživatel vedoucím
    var teamAsLead = await db.Teams
        .Include(t => t.LeadUser)
        .FirstOrDefaultAsync(t => t.LeadUserId == userId);

    // Pokud není vedoucí, najít tým kde je členem
    if (teamAsLead == null)
    {
        var membership = await db.TeamMemberships
            .Where(m => m.UserId == userId && (m.ToDate == null || m.ToDate >= today))
            .Include(m => m.Team)
            .ThenInclude(t => t.LeadUser)
            .FirstOrDefaultAsync();
        
        if (membership != null)
        {
            teamAsLead = membership.Team;
        }
    }

    if (teamAsLead == null)
        return Results.Ok(new { team = (object?)null, members = Array.Empty<object>() });

    // Načíst členy týmu
    var memberships = await db.TeamMemberships
        .Where(m => m.TeamId == teamAsLead.Id && (m.ToDate == null || m.ToDate >= today))
        .ToListAsync();

    var memberUserIds = memberships.Select(m => m.UserId).ToList();
    var memberUsers = await userManager.Users
        .Where(u => memberUserIds.Contains(u.Id))
        .ToListAsync();

    var members = memberships.Select(m =>
    {
        var user = memberUsers.FirstOrDefault(u => u.Id == m.UserId);
        return new
        {
            membershipId = m.Id,
            userId = m.UserId,
            userName = user?.FullName ?? user?.UserName ?? user?.Email ?? "Unknown",
            email = user?.Email,
            role = m.Role,
            fromDate = m.FromDate
        };
    }).ToList();

    return Results.Ok(new
    {
        team = new
        {
            id = teamAsLead.Id,
            name = teamAsLead.Name,
            leadUserId = teamAsLead.LeadUserId,
            leadUserName = teamAsLead.LeadUser?.FullName ?? teamAsLead.LeadUser?.UserName
        },
        members
    });
})
.WithTags("Teams")
.WithOpenApi();

// --- Admin: úprava uživatele (jméno, role, lockout, tým) ---
app.MapPut("/api/users/{id}", [Authorize(Roles = "Admin")] async (
    string id,
    [FromBody] UpdateUserRequest req,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    ApplicationDbContext db,
    ILogger<Program> logger
) =>
{
    var user = await users.FindByIdAsync(id);
    if (user is null)
        return Results.NotFound();

    // jméno
    if (!string.IsNullOrWhiteSpace(req.Name))
    {
        user.FullName = req.Name;
    }

    // role (Identity)
    if (req.Roles != null)
    {
        var currentRoles = await users.GetRolesAsync(user);

        var toRemove = currentRoles
            .Where(r => !req.Roles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var toAdd = req.Roles
            .Where(r => !currentRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (toRemove.Length > 0)
        {
            var removeRes = await users.RemoveFromRolesAsync(user, toRemove);
            if (!removeRes.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", removeRes.Errors.Select(e => e.Description)) });
        }

        foreach (var roleName in toAdd)
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                var roleRes = await roles.CreateAsync(new IdentityRole(roleName));
                if (!roleRes.Succeeded)
                    return Results.BadRequest(new { error = string.Join("; ", roleRes.Errors.Select(e => e.Description)) });
            }
        }

        if (toAdd.Length > 0)
        {
            var addRes = await users.AddToRolesAsync(user, toAdd);
            if (!addRes.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", addRes.Errors.Select(e => e.Description)) });
        }
    }

    // lockout
    if (req.Lockout.HasValue)
    {
        if (req.Lockout.Value)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(1);
        }
        else
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
        }
    }

    // Tým (TeamMembership)
    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    if (req.TeamId.HasValue)
    {
        var team = await db.Teams.FindAsync(req.TeamId.Value);
        if (team is null)
            return Results.BadRequest(new { error = "Zadaný tým neexistuje." });

        // ukončíme existující aktivní členství
        var activeMemberships = await db.TeamMemberships
            .Where(tm => tm.UserId == id && (tm.ToDate == null || tm.ToDate >= today))
            .ToListAsync();

        foreach (var m in activeMemberships)
        {
            // pokud je to stejné členství (stejný tým) a role se nemění, můžeme ho nechat být
            if (m.TeamId == req.TeamId.Value && (req.TeamRole == null || string.Equals(m.Role, req.TeamRole, StringComparison.OrdinalIgnoreCase)))
            {
                // nic neděláme
            }
            else
            {
                m.ToDate = today.AddDays(-1); // Musí být včera, ne dnes
            }
        }

        // existuje už členství v tomhle týmu s dnešním FromDate a nezavřeným ToDate?
        var hasSame = activeMemberships.Any(m => m.TeamId == req.TeamId.Value && (m.ToDate == null || m.ToDate >= today));
        if (!hasSame)
        {
            var newMembership = new TeamMembership
            {
                TeamId = req.TeamId.Value,
                UserId = id,
                FromDate = today,
                Role = string.IsNullOrWhiteSpace(req.TeamRole) ? "Member" : req.TeamRole
            };

            db.TeamMemberships.Add(newMembership);
        }
    }
    else
    {
        // žádný tým -> ukončíme všechna aktivní členství
        var activeMemberships = await db.TeamMemberships
            .Where(tm => tm.UserId == id && (tm.ToDate == null || tm.ToDate >= today))
            .ToListAsync();

        foreach (var m in activeMemberships)
        {
            m.ToDate = today.AddDays(-1); // Musí být včera, ne dnes
        }
    }

    var updateRes = await users.UpdateAsync(user);
    if (!updateRes.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", updateRes.Errors.Select(e => e.Description)) });

    await db.SaveChangesAsync();

    logger.LogInformation("Admin updated user {UserId}", user.Id);
    return Results.NoContent();
})
.WithTags("Admin")
.WithOpenApi();

// --- Admin: reset hesla uživatele ---
app.MapPost("/api/users/{id}/reset-password", [Authorize(Roles = "Admin")] async (
    string id,
    [FromBody] ResetUserPasswordRequest req,
    UserManager<ApplicationUser> users,
    ILogger<Program> logger
) =>
{
    var user = await users.FindByIdAsync(id);
    if (user is null)
        return Results.NotFound();

    if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
        return Results.BadRequest(new { error = "Heslo musí mít alespoň 6 znaků." });

    string token;
    try
    {
        token = await users.GeneratePasswordResetTokenAsync(user);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "GeneratePasswordResetTokenAsync failed for {UserId}", id);
        return Results.Problem(title: "Password reset failed", detail: ex.Message, statusCode: 500);
    }

    var resetRes = await users.ResetPasswordAsync(user, token, req.NewPassword);
    if (!resetRes.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", resetRes.Errors.Select(e => e.Description)) });

    logger.LogInformation("Admin reset password for user {UserId}", user.Id);
    return Results.NoContent();
})
.WithTags("Admin")
.WithOpenApi();

// --- Admin: smazat uživatele ---
app.MapDelete("/api/users/{id}", [Authorize(Roles = "Admin")] async (
    string id,
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    ILogger<Program> logger
) =>
{
    var user = await users.FindByIdAsync(id);
    if (user is null)
        return Results.NotFound(new { error = "Uživatel nenalezen." });

    // Zkontrolovat, že uživatel nemá výkazy
    var hasTimesheets = await db.Timesheets.AnyAsync(t => t.EmployeeId == id);
    if (hasTimesheets)
    {
        return Results.BadRequest(new { error = "Nelze smazat uživatele s existujícími výkazy. Místo toho ho zablokujte." });
    }

    // Smazat členství v týmech
    var memberships = await db.TeamMemberships.Where(tm => tm.UserId == id).ToListAsync();
    if (memberships.Any())
    {
        db.TeamMemberships.RemoveRange(memberships);
        await db.SaveChangesAsync();
    }

    // Smazat uživatele
    var result = await users.DeleteAsync(user);
    if (!result.Succeeded)
    {
        return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });
    }

    logger.LogInformation("Admin deleted user {UserId} ({Email})", id, user.Email);
    return Results.NoContent();
})
.WithTags("Admin")
.WithOpenApi();

await app.RunAsync();

public partial class Program { } // pro integrační testy

record LoginRequest(string Email, string Password);
record CreateUserRequest(string Email, string Password, string? Name, string[]? Roles);
record UpdateUserRequest(string? Name, string[]? Roles, bool? Lockout, Guid? TeamId, string? TeamRole);
record ResetUserPasswordRequest(string NewPassword);