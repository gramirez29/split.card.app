using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using SplitCard.Application.Abstractions;
using SplitCard.Infrastructure.Documents;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Serializers;
using SplitCard.Infrastructure.Repositories;
using SplitCard.Infrastructure.Security;

namespace SplitCard.Infrastructure;

public static class DependencyInjection
{
    private static bool _serializersRegistered;

    public static IServiceCollection AddSplitCardInfrastructure(this IServiceCollection services)
    {
        RegisterBsonSerializers();

        var mongoSettings = MongoDbSettings.FromEnvironment();
        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoDbContext>();

        var jwtSettings = JwtSettings.FromEnvironment();
        services.AddSingleton(jwtSettings);
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddSingleton<IStatementPdfGenerator, QuestPdfStatementGenerator>();

        services.AddScoped<IHouseholdRepository, MongoHouseholdRepository>();
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<ICardRepository, MongoCardRepository>();
        services.AddScoped<IInstallmentPlanRepository, MongoInstallmentPlanRepository>();
        services.AddScoped<ITransactionRepository, MongoTransactionRepository>();
        services.AddScoped<IStatementPeriodRepository, MongoStatementPeriodRepository>();
        services.AddScoped<ISplitRuleRepository, MongoSplitRuleRepository>();

        return services;
    }

    private static void RegisterBsonSerializers()
    {
        if (_serializersRegistered)
        {
            return;
        }

        BsonSerializer.RegisterSerializer(new DateOnlySerializer());
        _serializersRegistered = true;
    }
}
