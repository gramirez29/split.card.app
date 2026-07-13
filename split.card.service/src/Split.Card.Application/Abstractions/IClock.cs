namespace SplitCard.Application.Abstractions;

/// <summary>
/// Abstraction over "today", so date-sensitive Application logic (alerts, anything that
/// checks "is X days away from now") can be unit tested with a fixed date instead of
/// depending on whatever day the test happens to run on. Existing handlers that predate
/// this (e.g. GetHouseholdDashboardQueryHandler) still call DateTime.UtcNow directly —
/// not retrofitted yet, but new date-sensitive Application code should use this instead.
/// </summary>
public interface IClock
{
    DateOnly Today { get; }
}
