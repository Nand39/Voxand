using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GLAV.Types;

public class Texture2D : Texture
{
    /// <summary>
    /// Dimensions of the texture.
    /// </summary>
    public Vector2i Size { get; protected set; }
    public Texture2D(Vector2i textureSize, SizedInternalFormat storageFormat, int levels)
        : base(TextureTarget.Texture2D)
    {
        StorageFormat = (PixelInternalFormat)storageFormat;
        Size = textureSize;
        BindTex(0);
        GL.TexStorage2D(
            target: TextureTarget2d.Texture2D,
            levels: levels,
            internalformat: storageFormat,
            width: textureSize.X,
            height: textureSize.Y
        );
        SetParams(TexParam.defaultTexParams);
    }
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