using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Systems;

namespace GLAV.Types;
public class Shader : GLResource
{
    static Dictionary<Type, Action<int, object>> uniformSetters;
    static Shader()
    {
        uniformSetters = new Dictionary<Type, Action<int, object>>
    {
        { typeof(int), (loc, val) => GL.Uniform1(loc, (int)val) },
        { typeof(uint), (loc, val) => GL.Uniform1(loc, (uint)val) },
        { typeof(float), (loc, val) => GL.Uniform1(loc, (float)val) },
        { typeof(Vector2), (loc, val) => GL.Uniform2(loc, (Vector2)val) },
        { typeof(Vector2i), (loc, val) => GL.Uniform2(loc, (Vector2i)val) },
        { typeof(Vector3), (loc, val) => GL.Uniform3(loc, (Vector3)val) },
        { typeof(Vector3i), (loc, val) => GL.Uniform3(loc, (Vector3i)val) },
        { typeof(Vector4), (loc, val) => GL.Uniform4(loc, (Vector4)val) },
        { typeof(Vector4i), (loc, val) => GL.Uniform4(loc, (Vector4i)val) },
        { typeof(Color4), (loc, val) => GL.Uniform4(loc, (Color4)val) },
        { typeof(Quaternion), (loc, val) => GL.Uniform4(loc, (Quaternion)val) },
        { typeof(Matrix2), (loc, val) => { Matrix2 mat = (Matrix2)val; GL.UniformMatrix2(loc, true, ref mat); } },
        { typeof(Matrix3), (loc, val) => { Matrix3 mat = (Matrix3)val; GL.UniformMatrix3(loc, true, ref mat); } },
        { typeof(Matrix4), (loc, val) => { Matrix4 mat = (Matrix4)val; GL.UniformMatrix4(loc, true, ref mat); } },
    };
    }
    public Shader()
    {
        Handle.resourceType = GLResourceType.ShaderProgram;
        Handle.id = GL.CreateProgram();
    }
    public bool Create(params ShaderPart[] shaderAttachments)
    {
        for (int i = 0; i < shaderAttachments.Length; i++)
            GL.AttachShader(Handle.id, shaderAttachments[i].Handle.id);

        GL.LinkProgram(Handle.id);

        GL.GetProgram(Handle.id, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == (int)All.False)
            return false;

        for (int i = 0; i < shaderAttachments.Length; i++)
            GL.DetachShader(Handle.id, shaderAttachments[i].Handle.id);

        for (int i = 0; i < shaderAttachments.Length; i++)
            shaderAttachments[i].Dispose();

        return true;
    }
    public void Use() => GLRegistry.UseProgram(Handle.id);
    public void SetUniform<T>(int location, T value) where T : struct
    {
        if (uniformSetters.TryGetValue(typeof(T), out var setter))
        {
            Use();
            setter(location, value);
        }
        else
        {
            throw new ArgumentException($"Uniform type {typeof(T)} is not supported");
        }
    }

    #region Uniform setters per type
    public void SetUniform(int location, int value) { Use(); GL.Uniform1(location, value); }
    public void SetUniform(int location, uint value) { Use(); GL.Uniform1(location, value); }
    public void SetUniform(int location, float value) { Use(); GL.Uniform1(location, value); }
    public void SetUniform(int location, Vector2 value) { Use(); GL.Uniform2(location, value); }
    public void SetUniform(int location, Vector2i value) { Use(); GL.Uniform2(location, value); }
    public void SetUniform(int location, Vector3 value) { Use(); GL.Uniform3(location, value); }
    public void SetUniform(int location, Vector3i value) { Use(); GL.Uniform3(location, value); }
    public void SetUniform(int location, Vector4 value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Vector4i value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Color4 value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Quaternion value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Matrix2 value) { Use(); GL.UniformMatrix2(location, true, ref value); }
    public void SetUniform(int location, Matrix3 value) { Use(); GL.UniformMatrix3(location, true, ref value); }
    public void SetUniform(int location, Matrix4 value) { Use(); GL.UniformMatrix4(location, true, ref value); }
    #endregion
    public override void Free()
    {
        GL.DeleteBuffer(Handle.id);
    }
}