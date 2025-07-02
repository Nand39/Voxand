using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using StbImageSharp;

using GLAV.Types;
using Voxand.Helpers;
using System.Runtime.CompilerServices;
using GLAV.Helpers.Public.Exceptions;

namespace Voxand.Content;
public class ContentManager
{
    public string BasePath;

    static ContentManager()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public ContentManager(string indepBasePath)
    {
        BasePath = PlatformFilePath(indepBasePath);
    }

    FileStream OpenStream(string path, FileMode mode, FileAccess access) => new FileStream(CompleteFilePath(path), mode, access);


    #region Basic read/write string
    public string ReadFile(string path)
    {
        using Stream stream = OpenStream(path, FileMode.Open, FileAccess.Read);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
    public void WriteFile(string path, string text)
    {
        using Stream stream = OpenStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        using StreamWriter writer = new(stream);
        writer.Write(text);
    }
    #endregion

    #region Asset loading

    #region Texture2D
    public MutableTexture2D LoadTexture(string path, PixelInternalFormat storageFormat)
    {
        Stream stream;
        stream = OpenStream(path, FileMode.Open, FileAccess.Read);

        string? extension = Path.GetExtension(path);
        if (extension is null or "")
            throw new ArgumentException($"Cannot load texture at {path}; no file extension found.");

        MutableTexture2D texture;
        switch (extension)
        {
            default: throw new ArgumentException
                    ($"Cannot load image with format {extension}; supported image formats: .png, .jpg, .bmp, .tga, .hdr");
            
            case ".png":
            case ".jpg":
            case ".bmp":
            case ".tga":
                texture = LoadSimpleTexture(stream, storageFormat); break;

            case ".hdr": 
                texture = LoadHDRTexture(stream, storageFormat); break;
        }

        stream.Dispose();
        return texture;
    }

    MutableTexture2D LoadSimpleTexture(Stream imageFileStream, PixelInternalFormat storageFormat)
    {
        ImageResult image = ImageResult.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        return CreateTexture(new Vector2i(image.Width, image.Height), storageFormat, new(PixelFormat.Rgba, PixelType.UnsignedByte), image.Data);
    }

    MutableTexture2D LoadHDRTexture(Stream imageFileStream, PixelInternalFormat storageFormat)
    {
        ImageResultFloat image = ImageResultFloat.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        return CreateTexture(new Vector2i(image.Width, image.Height), storageFormat, new(PixelFormat.Rgba, PixelType.Float), image.Data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    MutableTexture2D CreateTexture<T>(Vector2i textureSize, PixelInternalFormat storageFormat, TexLoadFormat texLoadFormat, T[] data) where T : struct
    {
        MutableTexture2D texture = new MutableTexture2D();
        texture.Alloc(textureSize, storageFormat, texLoadFormat, data);
        return texture;
    }
    #endregion

    #region Shader
    public Shader LoadShader(params string[] sourcePaths)
    {
        ShaderPart[] shaderAttachments = new ShaderPart[sourcePaths.Length];
        for (int i = 0; i < shaderAttachments.Length; i++)
        {
            try
            {
                shaderAttachments[i] = LoadShaderPart(sourcePaths[i]);
            }
            catch (ShaderPartCompilationException ex)
            {
                throw new ArgumentException(
                    $"Failed to compile shader source code in {sourcePaths[i]}.\n" +
                    $"Log: {ex.InfoLog}", ex);
            }
        }

        try
        {
            Shader shader = new(shaderAttachments);
            return shader;
        }
        catch (ShaderLinkingException ex)
        {
            throw new ShaderLinkingException(
                @$"Failed to link shader.
                Parts:
                {string.Join(";\n", sourcePaths)}", ex.Message, ex);
        }
    }

    public ShaderPart LoadShaderPart(string sourcePath)
    {
        string source = ReadFile(sourcePath);

        ShaderType? type = Util.IdentifyShaderSource(sourcePath);

        if (type is null)
            throw new ArgumentException($"Cannot infer shader type from code file extension. Path: {sourcePath}");

        try
        {
            ShaderPart shaderPart = new ShaderPart(source, type.Value);
            return shaderPart;
        }
        catch (ShaderPartCompilationException ex)
        {
            throw new ShaderPartCompilationException($"Failed to compile shader source code in {sourcePath}", ex.InfoLog, ex);
        }
    }
    #endregion

    #endregion

    #region Formatting
    public string CompleteFilePath(string indepPath) => Path.Combine(BasePath, PlatformFilePath(indepPath));
    public string PlatformFilePath(string indepPath) => Path.Combine(indepPath.Split('/'));
    #endregion
}