using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Systems;

namespace GLAV.Types;
public readonly struct TexParam(TextureParameterName param, int value)
{
    public readonly TextureParameterName textureParameterName = param;
    public readonly int parameterValue = value;
}
public struct TextureFormat(PixelInternalFormat internalFormat, PixelFormat format, PixelType pixelType)
{
    public PixelInternalFormat internalFormat = internalFormat;
    public PixelFormat format = format;
    public PixelType pixelType = pixelType;
}

public class Texture2D : GLResource
{
    TextureUnit unit = TextureUnit.Texture0;
    public Texture2D()
    {
        Handle.resourceType = GLResourceType.Texture2D;
        Handle.id = GL.GenTexture();
    }
    public void Alloc(Vector2i textureSize, TextureFormat format, nint dataPtr)
    {
        GLRegistry.BindTexture(TextureTarget.Texture2D, Handle.id);
        GL.TexImage2D(TextureTarget.Texture2D, 0, format.internalFormat, textureSize.X, textureSize.Y, 0, format.format, format.pixelType, dataPtr);
    }

    public unsafe void Alloc<T>(Vector2i textureSize, TextureFormat format, ref T[] data)
    where T : struct
    {
        fixed (void* dataPtr = data)
        {
            Alloc(textureSize, format, (nint)dataPtr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Alloc<T>(Vector2i textureSize, TextureFormat format, ref T[,] data)
        where T : struct
    {
        fixed (void* dataPtr = data)
        {
            Alloc(textureSize, format, (nint)dataPtr);
        }
    }
    public void GenMipmaps()
    {
        GLRegistry.BindTexture(unit, TextureTarget.Texture2D, Handle.id);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
    public void BindToUnit(TextureUnit unit)
    {
        this.unit = unit;
        GLRegistry.BindTexture(unit, TextureTarget.Texture2D, Handle.id);
    }
    public void BindAsImage(int target, TextureAccess access, SizedInternalFormat format)
    {
        GL.BindImageTexture(target, Handle.id, 0, false, 0, access, format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetParam(TexParam param)
    {
        GLRegistry.BindTexture(unit, TextureTarget.Texture2D, Handle.id);
        GL.TexParameter(TextureTarget.Texture2D, param.textureParameterName, param.parameterValue);
    }
    public void SetParams(ref TexParam[] parameters)
    {
        GLRegistry.BindTexture(unit, TextureTarget.Texture2D, Handle.id);
        foreach (var param in parameters)
            SetParam(param);
    }
    public override void Free()
    {
        GL.DeleteTexture(Handle.id);
    }
}