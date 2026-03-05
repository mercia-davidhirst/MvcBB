namespace MvcBB.App.Exceptions;

/// <summary>
/// Exception thrown when a service operation fails
/// </summary>
public class ServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ServiceException class
    /// </summary>
    public ServiceException() { }

    /// <summary>
    /// Initializes a new instance of the ServiceException class with a specified error message
    /// </summary>
    public ServiceException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the ServiceException class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception
    /// </summary>
    public ServiceException(string message, Exception innerException) : base(message, innerException) { }
} 