namespace SplitCard.Application.Common;

public sealed class UnauthorizedException(string message) : Exception(message);
