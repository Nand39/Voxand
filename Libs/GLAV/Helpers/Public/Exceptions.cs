namespace GLAV.Helpers.Public.Exceptions;

public class ShaderCreationException : Exception
{
    public string InfoLog { get; private set; }
    public ShaderCreationException(string infoLog) => InfoLog = infoLog;

    public ShaderCreationException(string message, string infoLog)
        : base(message) => InfoLog = infoLog;

    public ShaderCreationException(string message, string infoLog, Exception innerException)
        : base(message, innerException) => InfoLog = infoLog;
}
public class ShaderPartCompilationException : ShaderCreationException
{
    public ShaderPartCompilationException(string infoLog) : base(infoLog) { }

    public ShaderPartCompilationException(string message, string infoLog)
        : base(message, infoLog) { }

    public ShaderPartCompilationException(string message, string infoLog, Exception innerException)
        : base(message, infoLog, innerException) { }
}

public class ShaderLinkingException : ShaderCreationException
{
    public ShaderLinkingException(string infoLog) : base(infoLog) { }

    public ShaderLinkingException(string message, string infoLog)
        : base(message, infoLog) { }

    public ShaderLinkingException(string message, string infoLog, Exception innerException)
        : base(message, infoLog, innerException) { }
}