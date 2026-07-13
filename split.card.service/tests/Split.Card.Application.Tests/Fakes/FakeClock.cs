using SplitCard.Application.Abstractions;

namespace Split.Card.Application.Tests.Fakes;

public sealed class FakeClock(DateOnly today) : IClock
{
    public DateOnly Today { get; } = today;
}
