namespace LocaGuest.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected DomainException(string code, string message, Exception innerException) 
        : base(message, innerException)
    {
        Code = code;
    }
}

public class ValidationException : DomainException
{
    public ValidationException(string code, string message) : base(code, message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string code, string message) : base(code, message) { }
}

public class ConflictException : DomainException
{
    public ConflictException(string code, string message) : base(code, message) { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string code, string message) : base(code, message) { }
}
