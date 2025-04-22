namespace Morphia.Core.Repositories.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message = "Resource not found") : base(message) {}

    public NotFoundException(List<string> message, string separator = "\n") : base(string.Join(separator, message)) {}
}