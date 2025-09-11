using Microsoft.AspNetCore.Diagnostics;

namespace dengue.watch.api.common.exceptions;

public class AuthExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is AuthException authException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = authException.Message }, cancellationToken: cancellationToken);
            return true;
        }
        return false;
    }
}