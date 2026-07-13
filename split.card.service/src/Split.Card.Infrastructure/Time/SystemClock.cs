using SplitCard.Application.Abstractions;

namespace SplitCard.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
