namespace SplitCard.Infrastructure.Security;

public sealed class JwtSettings
{
    public required string Secret { get; init; }
    public required int ExpirationMinutes { get; init; }

    public static JwtSettings FromEnvironment()
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("Environment variable JWT_SECRET is not set.");

        var expirationRaw = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");
        var expirationMinutes = int.TryParse(expirationRaw, out var parsed) ? parsed : 60 * 24 * 7; // 7 days default

        return new JwtSettings
        {
            Secret = secret,
            ExpirationMinutes = expirationMinutes
        };
    }
}
