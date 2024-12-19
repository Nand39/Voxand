namespace Voxand.Helpers.Exceptions;

public class FeatureUnsupportedException : Exception
{
    public FeatureUnsupportedException(string message)
        : base(message)
    {
    }
}