using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace dengue.watch.api.common.exceptions;

/// <summary>
/// Global exception handler implementing IExceptionHandler
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = validationEx.Message,
                Instance = httpContext.Request.Path,
                Extensions = { ["errors"] = validationEx.Errors }
            },

            NotFoundException notFoundEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Resource Not Found",
                Status = (int)HttpStatusCode.NotFound,
                Detail = notFoundEx.Message,
                Instance = httpContext.Request.Path
            },

            UnauthorizedException unauthorizedEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Unauthorized",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = unauthorizedEx.Message,
                Instance = httpContext.Request.Path
            },

            ForbiddenException forbiddenEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Forbidden",
                Status = (int)HttpStatusCode.Forbidden,
                Detail = forbiddenEx.Message,
                Instance = httpContext.Request.Path
            },

            ConflictException conflictEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflict",
                Status = (int)HttpStatusCode.Conflict,
                Detail = conflictEx.Message,
                Instance = httpContext.Request.Path
            },

            RegistrationFailedException registrationFailedEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Registration Failed",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = registrationFailedEx.Message,
                Instance = httpContext.Request.Path
            },
            UserAlreadyExistsException userAlreadyExistsEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "User Already Exists",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = userAlreadyExistsEx.Message,
                Instance = httpContext.Request.Path
            },
            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = httpContext.Request.Path
            }
        };
    }
}