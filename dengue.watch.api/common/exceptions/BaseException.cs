namespace dengue.watch.api.common.exceptions;

/// <summary>
/// Base exception class for application-specific exceptions
/// </summary>
public abstract class BaseException : Exception
{
    public string? Details { get; }

    protected BaseException(string message) : base(message) { }
    protected BaseException(string message, Exception innerException) : base(message, innerException) { }
    protected BaseException(string message, string? details, Exception? innerException = null) : base(message, innerException) 
    { 
        Details = details;
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : BaseException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message, Dictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : BaseException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key) : base($"Entity \"{name}\" ({key}) was not found.") { }
}

/// <summary>
/// Exception thrown when user is not authorized
/// </summary>
public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message) : base(message) { }
    public UnauthorizedException() : base("You are not authorized to perform this action.") { }
}

/// <summary>
/// Exception thrown when user is forbidden from accessing a resource
/// </summary>
public class ForbiddenException : BaseException
{
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException() : base("You do not have permission to access this resource.") { }
}

/// <summary>
/// Exception thrown when there's a conflict with the current state
/// </summary>
public class ConflictException : BaseException
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string name, object key) : base($"Entity \"{name}\" ({key}) already exists.") { }
}
