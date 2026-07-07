using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace SplitCard.Application.Cards;

public sealed record CreateCardCommand(
    string ActingUserId,
    string HouseholdId,
    string Name,
    string Bank,
    CardType Type,
    int? CutoffDay,
    int? PaymentDueDay);

/// <summary>
/// Only the household Owner manages cards. Contributor/RestrictedViewer can read but not
/// create/edit them — matches the permission model where Cards are shared infrastructure,
/// not something each member configures independently.
/// </summary>
public sealed class CreateCardCommandHandler(ICardRepository cardRepository, IUserRepository userRepository)
{
    public async Task<Card> Handle(CreateCardCommand command, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(command.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {command.ActingUserId} not found.");

        if (actingUser.Role != UserRole.Owner)
        {
            throw new ForbiddenException("Only the household Owner can register cards.");
        }

        var card = new Card(
            IdGenerator.NewId(),
            command.HouseholdId,
            command.Name,
            command.Bank,
            command.Type,
            command.CutoffDay,
            command.PaymentDueDay,
            command.ActingUserId);

        await cardRepository.AddAsync(card, cancellationToken);

        return card;
    }
}
