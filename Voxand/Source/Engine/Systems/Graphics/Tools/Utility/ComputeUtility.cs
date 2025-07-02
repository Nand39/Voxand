using GLAV.Types;
using GLAV.Helpers.Public.Exceptions;

using Voxand.Content;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using OpenTK.Graphics.OpenGL4;

namespace Voxand.Engine.Systems.Graphics.Tools.Utility;
public class ComputeUtility
{
    public static ComputeUtility Instance { get; private set; }

    ShaderController copyTex8Shader;

    ComputeUtility() { }
    public static void Initialize(ContentManager content, (string codePath, bool embedded) copyTex8ShaderPath)
    {
        Instance = new ComputeUtility()
        {
            copyTex8Shader = new(content, copyTex8ShaderPath)
        };
    }

    public void CopyTex8(MutableTexture2D source, MutableTexture2D destination, SizedInternalFormat sizedInternalFormat, bool imageBarrier)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(source, 8);
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(source, destination);

        copyTex8Shader.Shader.Use();
        copyTex8Shader.SetUniform("source", 0);
        copyTex8Shader.SetUniform("destination", 0);

        source.BindTex(0);
        destination.BindAsImage(0, TextureAccess.WriteOnly);
        
        GL.DispatchCompute(source.Size.X / 8, source.Size.Y / 8, 1);
        if (imageBarrier)
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
    }
}