namespace Voxand.Helpers.Exceptions;
public class ServiceNotFoundException<I> : Exception where I : class
{
    public ServiceNotFoundException()
        : base($"Service '{typeof(I)}' not found.")
    {
    }
    public ServiceNotFoundException(string message)
        : base(message)
    {
    }
    public ServiceNotFoundException(Exception innerException)
    : base($"Service '{typeof(I)}' not found.", innerException)
    {
    }
    public ServiceNotFoundException(string message, Exception innerException)
    : base(message, innerException)
    {
    }
}