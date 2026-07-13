using System.Text;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SplitCard.Api.Endpoints;
using SplitCard.Api.ErrorHandling;
using SplitCard.Application;
using SplitCard.Infrastructure;
using SplitCard.Infrastructure.Persistence;

// Local dev only: loads src/Split.Card.Api/.env into process environment variables
// before anything reads them via Environment.GetEnvironmentVariable. No-op on Railway —
// there's no .env file in the deployed container, real env vars are injected by the
// platform instead. Must run before AddSplitCardInfrastructure(), which reads
// MONGODB_CONNECTION_STRING/MONGODB_DATABASE_NAME synchronously at registration time.
var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");

if (File.Exists(envFilePath))
{
    Env.Load(envFilePath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSplitCardInfrastructure();
builder.Services.AddSplitCardApplication();

// Enums as strings in JSON (not the default numeric encoding). Resolves the
// JsonStringEnumConverter decision — split.card.mobile's src/types/enums.ts already
// assumes string values.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("Environment variable JWT_SECRET is not set.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, ASP.NET Core remaps short claim names ("sub", "email") to long
        // ClaimTypes URIs on the way in. JwtTokenGenerator issues "sub" as-is;
        // ClaimsPrincipalExtensions.GetUserId() reads "sub" as-is. Keep both sides in sync.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SplitCard API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Swagger enabled in every environment (including Railway) so endpoints can be verified
// against the deployed API, not just locally. Revisit if this needs to be gated behind
// an env var once real user data exists in the production database.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SplitCard API v1");
});

app.UseAuthentication();
app.UseAuthorization();

// Verifica conexión real a Mongo, no solo que el proceso esté vivo (Skill 3, regla 4:
// manejo robusto de caídas/timeouts de Mongo).
app.MapGet("/health", async (MongoDbContext context, CancellationToken cancellationToken) =>
{
    var mongoIsHealthy = await context.PingAsync(cancellationToken);

    return mongoIsHealthy
        ? Results.Ok(new { status = "healthy", mongo = "connected", timestampUtc = DateTime.UtcNow })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
})
.WithTags("Health");

app.MapAuthEndpoints();
app.MapHouseholdEndpoints();
app.MapUserEndpoints();
app.MapCardEndpoints();
app.MapTransactionEndpoints();
app.MapSplitRuleEndpoints();
app.MapReconciliationEndpoints();
app.MapDashboardEndpoints();
app.MapAlertsEndpoints();

app.Run();
