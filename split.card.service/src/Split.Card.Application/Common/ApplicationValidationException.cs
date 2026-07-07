namespace SplitCard.Application.Common;

/// <summary>
/// Signals an application-level input validation failure (e.g. duplicate email, invalid
/// role transition) — distinct from SplitCard.Domain.Exceptions.DomainException, which
/// guards entity invariants. Controllers should map this to HTTP 400.
/// </summary>
public sealed class ApplicationValidationException(string message) : Exception(message);
