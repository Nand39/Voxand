using GLAV.Helpers.Public.Exceptions;
using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types;
public class ShaderPart : GLResource
{
    public ShaderPart(string source, ShaderType type)
    {
        Handle.resourceType = GLResourceType.ShaderPart;
        Handle.id = GL.CreateShader(type);
        Label = "Unnamed shader part";

        GL.ShaderSource(Handle.id, source);
        GL.CompileShader(Handle.id);

        if (GetParameter(ShaderParameter.CompileStatus) != (int)All.True)
            throw new ShaderPartCompilationException(GetInfoLog());
    }

    public int GetParameter(ShaderParameter param)
    {
        GL.GetShader(Handle.id, param, out int result);
        return result;
    }

    public string GetInfoLog() => GL.GetShaderInfoLog(Handle.id);

    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteShader(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteShader(Handle.id)); 
    }
}