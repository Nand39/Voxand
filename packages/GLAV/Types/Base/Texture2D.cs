using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Systems;

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
public struct TextureFormat(PixelInternalFormat internalFormat, PixelFormat format, PixelType pixelType)
{
    public PixelInternalFormat internalFormat = internalFormat;
    public PixelFormat format = format;
    public PixelType pixelType = pixelType;
}
public class Texture2D : GLResource
{
    public int TextureUnit { get; protected set; } = -1;
    public int ImageUnit { get; protected set; } = -1;

    /// <summary>
    /// Dimensions of the texture.
    /// </summary>
    public Vector2i Size { get; protected set; }

    /// <summary>
    /// Storage format of the texture.
    /// </summary>
    public TextureFormat Format { get; protected set; }

    public Texture2D()
    {
        Handle.resourceType = GLResourceType.Texture2D;
        Handle.id = GL.GenTexture();
    }

    #region Allocation
    public void Alloc(Vector2i textureSize, TextureFormat format, nint dataPtr)
    {
        BindTex(0);
        GL.TexImage2D(TextureTarget.Texture2D, 0, format.internalFormat, textureSize.X, textureSize.Y, 0, format.format, format.pixelType, dataPtr);
        Size = textureSize; Format = format; 
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
    #endregion

    #region Binding
    public void BindTex(int unit)
    {
        TextureUnit = unit;
        GLRegistry.Instance.BindTexture(this, unit, TextureTarget.Texture2D);
    }
    public void BindAsImage(int target, TextureAccess access, SizedInternalFormat format)
    {
        GLRegistry.Instance.BindImage(this, target, access, format);
        ImageUnit = target;
    }
    public void MarkTextureUnbound() => TextureUnit = -1;
    public void MarkImageUnbound() => ImageUnit = -1;
    #endregion

    #region Parameters
    public void SetParam(TexParam param)
    {
        BindTex(0);
        GL.TexParameter(TextureTarget.Texture2D, param.textureParameterName, param.parameterValue);
    }
    public void SetParams(ref TexParam[] parameters)
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
    public void Realloc(Vector2i textureSize) => Alloc(textureSize, Format, nint.Zero);
    public override string ToString() => $"\"{Lable}\" (id={Handle.id})";
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