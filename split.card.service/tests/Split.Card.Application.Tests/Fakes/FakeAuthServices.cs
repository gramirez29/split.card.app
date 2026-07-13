using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;

namespace Split.Card.Application.Tests.Fakes;

/// <summary>
/// Deliberately not real crypto — this is a fake for Application-layer tests only.
/// The real PBKDF2 implementation lives in Split.Card.Infrastructure and is not
/// referenced here, to keep this test project isolated to Domain + Application.
/// </summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
}

public sealed class FakeTokenGenerator : ITokenGenerator
{
    public string GenerateToken(User user) => $"fake-token-for-{user.Id}";
}
