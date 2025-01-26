namespace GLAV.Helpers.Public.Exceptions;
public class ShaderPartCompilationException : Exception
{
    public ShaderPartCompilationException() { }

    public ShaderPartCompilationException(string message) : base(message) { }

    public ShaderPartCompilationException(string message, Exception innerException) : base(message, innerException) { }
}

public class ShaderPartLinkingException : Exception
{
    public ShaderPartLinkingException() { }

    public ShaderPartLinkingException(string message) : base(message) { }

    public ShaderPartLinkingException(string message, Exception innerException) : base(message, innerException) { }
}
