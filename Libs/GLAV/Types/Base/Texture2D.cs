using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GLAV.Types;
public readonly struct TexParam(TextureParameterName param, int value)
{
    public static readonly TexParam[] defaultTexParams = 
        [
        new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest),
        new(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest),
        new(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge),
        new(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge),
        ];
    public readonly TextureParameterName textureParameterName = param;
    public readonly int parameterValue = value;
}
public struct TexLoadFormat(PixelFormat channels, PixelType pixelType)
{
    public PixelFormat channels = channels;
    public PixelType pixelType = pixelType;
}
public class Texture2D : GLResource
{

    /// <summary>
    /// Dimensions of the texture.
    /// </summary>
    public Vector2i Size { get; protected set; }

    /// <summary>
    /// Storage format of the texture.
    /// </summary>
    public PixelInternalFormat StorageFormat { get; protected set; } = PixelInternalFormat.Rgba32f;

    public Texture2D()
    {
        Handle.resourceType = GLResourceType.Texture2D;
        Handle.id = GL.GenTexture();
        SetParams(TexParam.defaultTexParams);
    }

    #region Allocation
    public void Alloc(Vector2i textureSize, PixelInternalFormat storageFormat, TexLoadFormat sourceFormat, nint sourcePtr)
    {
        BindTex(0);
        GL.TexImage2D(TextureTarget.Texture2D, 0, storageFormat, textureSize.X, textureSize.Y, 0, sourceFormat.channels, sourceFormat.pixelType, sourcePtr);
        Size = textureSize; 
        StorageFormat = storageFormat; 
    }

    public unsafe void Alloc<T>(Vector2i textureSize, PixelInternalFormat storageFormat, TexLoadFormat sourceFormat, T[] data)
        where T : struct
    {
        fixed (void* dataPtr = data)
        {
            Alloc(textureSize, storageFormat, sourceFormat, (nint)dataPtr);
        }
    }

    public unsafe void Alloc<T>(Vector2i textureSize, PixelInternalFormat storageFormat, TexLoadFormat sourceFormat, T[,] data)
        where T : struct
    {
        fixed (void* dataPtr = data)
        {
            Alloc(textureSize, storageFormat, sourceFormat, (nint)dataPtr);
        }
    }

    public void Alloc(Vector2i textureSize, PixelInternalFormat storageFormat)
    {
        Alloc(textureSize, storageFormat, GetSuitableTexLoadFormat(storageFormat), nint.Zero);
    }

    public void Realloc(Vector2i textureSize) => Alloc(textureSize, StorageFormat);
    #endregion

    #region Binding
    public void BindTex(int unit)
    {
        GLRegistry.Instance.BindTexture(this, unit, TextureTarget.Texture2D);
    }
    public void BindAsImage(int target, TextureAccess access, SizedInternalFormat format)
    {
        GLRegistry.Instance.BindImage(this, target, access, format);
    }
    #endregion

    #region Parameters
    public void SetParam(TexParam param)
    {
        BindTex(0);
        GL.TexParameter(TextureTarget.Texture2D, param.textureParameterName, param.parameterValue);
    }
    public void SetParams(TexParam[] parameters)
    {
        BindTex(0);
        foreach (var param in parameters)
            SetParam(param);
    }
    #endregion
    
    public void GenMipmaps()
    {
        BindTex(0);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    //Written by chatGPT
    public TexLoadFormat GetSuitableTexLoadFormat(PixelInternalFormat storageFormat)
    {
        return storageFormat switch
        {
            // Float formats
            PixelInternalFormat.Rgba32f => new TexLoadFormat(PixelFormat.Rgba, PixelType.Float),
            PixelInternalFormat.Rgb32f => new TexLoadFormat(PixelFormat.Rgb, PixelType.Float),
            PixelInternalFormat.Rgba16f => new TexLoadFormat(PixelFormat.Rgba, PixelType.HalfFloat),
            PixelInternalFormat.Rgb16f => new TexLoadFormat(PixelFormat.Rgb, PixelType.HalfFloat),

            // Unsigned normalized formats
            PixelInternalFormat.Rgba8 => new TexLoadFormat(PixelFormat.Rgba, PixelType.UnsignedByte),
            PixelInternalFormat.Rgb8 => new TexLoadFormat(PixelFormat.Rgb, PixelType.UnsignedByte),

            // Integer formats
            PixelInternalFormat.Rgba8i => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.Byte),
            PixelInternalFormat.Rgba8ui => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.UnsignedByte),
            PixelInternalFormat.Rgb8i => new TexLoadFormat(PixelFormat.RgbInteger, PixelType.Byte),
            PixelInternalFormat.Rgb8ui => new TexLoadFormat(PixelFormat.RgbInteger, PixelType.UnsignedByte),
            PixelInternalFormat.Rgba32i => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.Int),
            PixelInternalFormat.Rgba32ui => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.UnsignedInt),

            // Depth formats
            PixelInternalFormat.DepthComponent24 => new TexLoadFormat(PixelFormat.DepthComponent, PixelType.UnsignedInt),
            PixelInternalFormat.DepthComponent32f => new TexLoadFormat(PixelFormat.DepthComponent, PixelType.Float),

            // Depth-stencil formats
            PixelInternalFormat.Depth24Stencil8 => new TexLoadFormat(PixelFormat.DepthStencil, PixelType.UnsignedInt248),
            PixelInternalFormat.Depth32fStencil8 => new TexLoadFormat(PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev),

            _ => throw new ArgumentException($"Unsupported storage format: {storageFormat}")
        };
    }

    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteTexture(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteTexture(Handle.id));
    }
}