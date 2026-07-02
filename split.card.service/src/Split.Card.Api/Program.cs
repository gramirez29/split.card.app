using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SplitCard.Infrastructure;
using SplitCard.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSplitCardInfrastructure();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("Environment variable JWT_SECRET is not set.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
builder.Services.AddControllers();

var app = builder.Build();

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
});

app.MapControllers();

app.Run();
