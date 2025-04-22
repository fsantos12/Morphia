namespace Morphia.Core.Repositories.Exceptions;

public class ModelInvalidException : InvalidException
{
    public ModelInvalidException(string message = "Model is invalid") : base(message) {}

    public ModelInvalidException(List<string> message, string separator = "\n") : base(string.Join(separator, message)) {}
}