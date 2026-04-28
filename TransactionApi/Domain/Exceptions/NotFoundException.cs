namespace TransactionApi.Domain.Exceptions;

/// <summary>Represents an error raised when a requested resource cannot be found.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
