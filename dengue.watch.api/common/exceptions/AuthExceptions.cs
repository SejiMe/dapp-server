namespace dengue.watch.api.common.exceptions;

/// <summary>
/// Base authentication exception
/// </summary>
public abstract class AuthException : BaseException
{
    protected AuthException(string message, string? details = null, Exception? innerException = null) 
: base(message, details, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when authentication fails
/// </summary>
public class AuthenticationFailedException : AuthException
{
    public AuthenticationFailedException(string message = "Authentication failed", string? details = null, Exception? innerException = null) 
        : base(message, details, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when user registration fails
/// </summary>
public class RegistrationFailedException : AuthException
{
    public RegistrationFailedException(string message = "User registration failed", string? details = null, Exception? innerException = null) 
        : base(message, details, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when token operations fail
/// </summary>
public class TokenException : AuthException
{
    public TokenException(string message = "Token operation failed", string? details = null, Exception? innerException = null) 
        : base(message, details, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when token is invalid or expired
/// </summary>
public class InvalidTokenException : TokenException
{
    public InvalidTokenException(string message = "Invalid or expired token", string? details = null, Exception? innerException = null) 
        : base(message, details, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when user already exists
/// </summary>
public class UserAlreadyExistsException : AuthException
{
    public UserAlreadyExistsException(string email) 
        : base("User already exists", $"A user with email '{email}' already exists")
    {
    }
}

/// <summary>
/// Exception thrown when user is not found
/// </summary>
public class UserNotFoundException : AuthException
{
    public UserNotFoundException(string identifier) 
        : base("User not found", $"User with identifier '{identifier}' was not found")
    {
    }
}

/// <summary>
/// Exception thrown when email confirmation is required
/// </summary>
public class EmailConfirmationRequiredException : AuthException
{
    public EmailConfirmationRequiredException() 
        : base("Email confirmation required", "Please check your email and confirm your account before signing in")
    {
    }
}
