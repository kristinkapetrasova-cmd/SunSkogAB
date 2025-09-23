using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SunSkog.Api.Health;

var builder = WebApplication.CreateBuilder(args);

// Serilog – loguje do konzole (čitelné v Dockeru i v CI)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Health checks – self + DB
builder.Services
    .AddHealthChecks()
    .AddCheck<DbHealthCheck>("database");

// === DbContext (SQL Server) ===
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Identity ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// === JWT auth ===
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key in configuration");
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// === FluentValidation ===
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// === Swagger + Bearer ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SunSkog API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Zadej: Bearer {JWT}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Strukturované logy každého requestu
app.UseSerilogRequestLogging();

// Globální handler chyb -> vrací ProblemDetails (application/problem+json)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected error",
            Detail = app.Environment.IsDevelopment() ? ex?.Message : "Something went wrong."
        };

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

// Health endpoints
app.MapHealthChecks("/health/"); // liveness (jednoduché OK/KO)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    // readiness – vrátí JSON s položkami
    ResponseWriter = HealthResponseWriter.WriteJson,
    Predicate = _ => true
});


// === Mapování endpointů (nech jen to, co opravdu máš v projektu) ===
SunSkog.Api.Auth.AuthEndpoints.Map(app);
SunSkog.Api.Endpoints.TimesheetEndpoints.Map(app);
SunSkog.Api.Endpoints.TimesheetEntryEndpoints.Map(app);
SunSkog.Api.Endpoints.AdminTimesheetEndpoints.Map(app);
SunSkog.Api.Endpoints.AdminTimesheetDetailEndpoints.Map(app);
SunSkog.Api.Endpoints.AdminTimesheetExportEndpoints.Map(app);
SunSkog.Api.Endpoints.AdminDashboardEndpoints.Map(app);
SunSkog.Api.Endpoints.AdminExportEndpoints.Map(app);
SunSkog.Api.Endpoints.AdminReportsEndpoints.Map(app);

app.Run();