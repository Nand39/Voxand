using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types;

public abstract class Texture : GLResource
{
    public TextureTarget TextureTarget { get; }
    public PixelInternalFormat StorageFormat { get; protected set; }
    public Texture(TextureTarget target)
    {
        Handle.resourceType = GLResourceType.Texture;
        Handle.id = GL.GenTexture();
        TextureTarget = target;
        BindTex(0);
    }

    public void BindTex(int unit)
    {
        GLRegistry.Instance.BindTexture(this, unit, TextureTarget);
    }

    public void BindAsImage(int target, TextureAccess access)
    {
        if (!Enum.IsDefined((SizedInternalFormat)StorageFormat))
            throw new InvalidOperationException($"Cannot bind texture {ToString()} with storage format {StorageFormat} to image unit. Image units only support formats from {nameof(SizedInternalFormat)} enum.");

        GLRegistry.Instance.BindImage(this, target, access, (SizedInternalFormat)StorageFormat);
    }

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

    protected sealed override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteTexture(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteTexture(Handle.id));
    }
}
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

public enum CompressedStorageFormats
{
    CompressedRgbS3tcDxt1Ext = 33776,
    CompressedRgbaS3tcDxt1Ext = 33777,
    CompressedRgbaS3tcDxt3Ext = 33778,
    CompressedRgbaS3tcDxt5Ext = 33779,

    CompressedSrgbS3tcDxt1Ext = 35916,
    CompressedSrgbAlphaS3tcDxt1Ext = 35917,
    CompressedSrgbAlphaS3tcDxt3Ext = 35918,
    CompressedSrgbAlphaS3tcDxt5Ext = 35919,

    CompressedRedRgtc1 = 36283,
    CompressedSignedRedRgtc1 = 36284,
    CompressedRgRgtc2 = 36285,
    CompressedSignedRgRgtc2 = 36286,

    CompressedRgbaBptcUnorm = 36492,
    CompressedSrgbAlphaBptcUnorm = 36493,
    CompressedRgbBptcSignedFloat = 36494,
    CompressedRgbBptcUnsignedFloat = 36495,
}