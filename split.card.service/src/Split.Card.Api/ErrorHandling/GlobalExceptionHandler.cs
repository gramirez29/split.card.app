using Microsoft.AspNetCore.Diagnostics;
using SplitCard.Application.Common;
using SplitCard.Domain.Exceptions;

namespace SplitCard.Api.ErrorHandling;

/// <summary>
/// Central exception -> HTTP status mapping for every Minimal API endpoint. Endpoints
/// never catch these exceptions themselves — they let the handler throw and this
/// middleware translates it. Keeps endpoint bodies free of repetitive try/catch.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ApplicationValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain rule violation"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        // CancellationToken.None here, not the incoming cancellationToken: if the client
        // has already disconnected, that token is canceled and WriteAsJsonAsync would throw
        // OperationCanceledException from inside the exception handler itself, escaping
        // TryHandleAsync unhandled instead of just skipping a write nobody will receive.
        try
        {
            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    title,
                    status = statusCode,
                    detail = exception.Message
                },
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }

        return true;
    }
}
