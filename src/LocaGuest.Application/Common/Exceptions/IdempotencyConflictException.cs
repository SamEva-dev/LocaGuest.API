namespace LocaGuest.Application.Common.Exceptions;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message) : base(message) { }
}
