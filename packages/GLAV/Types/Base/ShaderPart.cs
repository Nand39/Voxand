using GLAV.Systems;
using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types;
public class ShaderPart : GLResource
{
    public ShaderPart(string source, ShaderType type)
    {
        Handle.resourceType = GLResourceType.ShaderPart;
        Handle.id = GL.CreateShader(type);

        GL.ShaderSource(Handle.id, source);
        GL.CompileShader(Handle.id);

        GL.GetShader(Handle.id, ShaderParameter.CompileStatus, out int compileStatus);
        if (compileStatus == (int)All.False)
        {
            string infoLog = GL.GetShaderInfoLog(Handle.id);
            throw new Exception(infoLog);
        }
    }
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