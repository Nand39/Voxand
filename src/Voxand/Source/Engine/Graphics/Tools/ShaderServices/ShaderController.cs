using DisposableExt;

using GLAV.Types;
using System.Diagnostics;
using Voxand.Content;

namespace Voxand.Engine.Graphics.Tools.ShaderServices;
public class ShaderController : IDisposableExt
{
    public Shader Shader { get; protected set; }
    public ShaderInfo ShaderInfo { get; protected set; }
    public DisposeHelper DisposeHelper { get; }

    public ShaderController(ContentManager content, bool embedded, params string[] sourcePaths)
    {
        Shader = content.LoadShader(embedded, sourcePaths);
        ShaderInfo = new ShaderInfo(Shader);
        DisposeHelper = new(this);
    }
    public ShaderController(Shader shader)
    {
        Shader = shader;
        ShaderInfo = new ShaderInfo(Shader);
    }
    public void SetUniform(string uniformName, object value)
    {
        UniformInfo? info = ShaderInfo.GetUniformInfo(uniformName);
        if (info is null) 
        {
            Console.WriteLine($"Failed to set uniform \"{uniformName}\" of shader {Shader.Handle.id}; Not found.");
            return; 
        }
        Shader.SetUniform(info.Value.location, info.Value.type, value);
    }
    public void SetBufferBinding(string bufferName, int binding)
    {
        int? index = ShaderInfo.GetShaderStorageBufferIndex(bufferName);
        if (index is null)
        {
            //Console.WriteLine($"Failed to set binding of buffer \"{bufferName}\" of shader {Shader.Handle.id}; Not found.");
            return;
        }
        Shader.SetShaderStorageBufferBinding(index.Value, binding);
    }

    void IDisposableExt.Free()
    {
        Shader.Dispose();
        GC.SuppressFinalize(this);
    }

    ~ShaderController() => this.Dispose();
}