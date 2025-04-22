namespace Morphia.Core.Repositories.Exceptions;

public class InvalidException : Exception
{
    public InvalidException(string message = "Operation is invalid") : base(message) {}

    public InvalidException(List<string> message, string separator = "\n") : base(string.Join(separator, message)) {}
}