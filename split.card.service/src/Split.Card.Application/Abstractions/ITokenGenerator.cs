using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
