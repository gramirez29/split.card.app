namespace SplitCard.Application.Common;

public sealed class ForbiddenException(string message) : Exception(message);
