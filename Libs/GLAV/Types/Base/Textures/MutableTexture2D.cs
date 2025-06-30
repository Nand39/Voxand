using GLAV.Helpers.Internal;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GLAV.Types;

public class MutableTexture2D : Texture
{
    /// <summary>
    /// Dimensions of the texture.
    /// </summary>
    public Vector2i Size { get; protected set; }

    public MutableTexture2D() : base(TextureTarget.Texture2D)
    {
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
        Alloc(textureSize, storageFormat, Util.GetSuitableTexLoadFormat(storageFormat), nint.Zero);
    }

    public void Realloc(Vector2i textureSize) => Alloc(textureSize, StorageFormat);
    #endregion

    public void Store(int targetLevel, Vector2i regionOffset, Vector2i regionDimentions, TexLoadFormat loadFormat, nint dataPtr)
    {
        BindTex(0);
        GL.TexSubImage2D(
            target: TextureTarget.Texture2D,
            level: targetLevel,
            xoffset: regionOffset.X,
            yoffset: regionOffset.Y,
            width: regionDimentions.X,
            height: regionDimentions.Y,
            format: loadFormat.channels,
            type: loadFormat.pixelType,
            pixels: dataPtr
        );
    }
    public unsafe void Store<T>(int targetLevel, Vector2i regionOffset, Vector2i regionDimentions, TexLoadFormat loadFormat, T[] data)
        where T : struct
    {
        fixed (void* dataPtr = data)
        {
            Store(targetLevel, regionOffset, regionDimentions, loadFormat, (nint)dataPtr);
        }
    }
    public unsafe void Store<T>(int targetLevel, Vector2i regionOffset, Vector2i regionDimentions, TexLoadFormat loadFormat, T[,] data)
        where T : struct
    {
        fixed (void* dataPtr = data)
        {
            Store(targetLevel, regionOffset, regionDimentions, loadFormat, (nint)dataPtr);
        }
    }

    public void GenMipmaps()
    {
        BindTex(0);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
}