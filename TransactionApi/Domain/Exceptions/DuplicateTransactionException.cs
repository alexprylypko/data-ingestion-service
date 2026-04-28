namespace TransactionApi.Domain.Exceptions;

/// <summary>Represents an error raised when a transaction violates the deduplication constraint.</summary>
public sealed class DuplicateTransactionException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DuplicateTransactionException"/>.
    /// </summary>
    public DuplicateTransactionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DuplicateTransactionException"/> with an inner exception.
    /// </summary>
    public DuplicateTransactionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
