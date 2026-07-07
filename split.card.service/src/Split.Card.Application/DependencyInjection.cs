using Microsoft.Extensions.DependencyInjection;
using SplitCard.Application.Auth;
using SplitCard.Application.Cards;
using SplitCard.Application.Households;
using SplitCard.Application.SplitRules;
using SplitCard.Application.Transactions;
using SplitCard.Application.Users;

namespace SplitCard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSplitCardApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();

        services.AddScoped<RegisterHouseholdCommandHandler>();
        services.AddScoped<InviteUserCommandHandler>();

        services.AddScoped<CreateCardCommandHandler>();
        services.AddScoped<GetCardsForHouseholdQueryHandler>();

        services.AddScoped<RegisterTransactionCommandHandler>();
        services.AddScoped<GetVisibleTransactionsQueryHandler>();
        services.AddScoped<GetTransactionsForCardPeriodQueryHandler>();

        services.AddScoped<CreateSplitRuleCommandHandler>();
        services.AddScoped<GetSplitRulesForHouseholdQueryHandler>();
        services.AddScoped<SuggestSplitForMerchantQueryHandler>();

        return services;
    }
}
