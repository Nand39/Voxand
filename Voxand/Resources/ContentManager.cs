using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using StbImageSharp;

using GLAV.Types;
using Voxand.Helpers;
using System.Runtime.CompilerServices;

namespace Voxand.Content;
public class ContentManager
{
    readonly string basePath;

    static ContentManager()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public ContentManager(string indepBasePath)
    {
        basePath = PlatformFilePath(indepBasePath);
    }

    FileStream OpenStream(string path, FileMode mode, FileAccess access) => new FileStream(CompleteFilePath(path), mode, access);


    #region Basic read/write string
    public bool ReadFile(string path, out string result)
    {
        try
        {
            using Stream stream = OpenStream(path, FileMode.Open, FileAccess.Read);
            using StreamReader reader = new(stream);
            result = reader.ReadToEnd();
            return true;
        }
        catch
        {
            result = string.Empty;
            return false;
        }
    }
    public bool WriteFile(string path, string text)
    {
        try
        {
            using Stream stream = OpenStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            using StreamWriter writer = new(stream);
            writer.Write(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region Asset loading

    #region Texture2D
    public Texture2D LoadTexture(string path, PixelInternalFormat storageFormat)
    {
        Stream stream;
        stream = OpenStream(path, FileMode.Open, FileAccess.Read);

        string? extension = Path.GetExtension(path);
        if (extension is null or "")
            throw new ArgumentException($"Cannot load texture at {path}; no file extension found.");

        Texture2D texture;
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

    Texture2D LoadSimpleTexture(Stream imageFileStream, PixelInternalFormat storageFormat)
    {
        ImageResult image = ImageResult.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        return CreateTexture(new Vector2i(image.Width, image.Height), storageFormat, new(PixelFormat.Rgba, PixelType.UnsignedByte), image.Data);
    }

    Texture2D LoadHDRTexture(Stream imageFileStream, PixelInternalFormat storageFormat)
    {
        ImageResultFloat image = ImageResultFloat.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        return CreateTexture(new Vector2i(image.Width, image.Height), storageFormat, new(PixelFormat.Rgba, PixelType.UnsignedByte), image.Data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Texture2D CreateTexture<T>(Vector2i textureSize, PixelInternalFormat storageFormat, TexLoadFormat texLoadFormat, T[] data) where T : struct
    {
        Texture2D texture = new Texture2D();
        texture.Alloc(textureSize, storageFormat, texLoadFormat, data);
        return texture;
    }
    #endregion

    #region Shader
    public Shader LoadShader(params string[] sourcePaths)
    {
        ShaderPart[] shaderAttachments = new ShaderPart[sourcePaths.Length];
        for (int i = 0; i < shaderAttachments.Length; i++)
            shaderAttachments[i] = LoadShaderPart(sourcePaths[i]);

        try
        {
            Shader shader = new(shaderAttachments);
            return shader;
        }
        catch (Exception e)
        {
            throw new Exception(
                @$"Failed to create shader.
                Log: {e.Message}
                Parts:
                {string.Join(";\n", sourcePaths)}", e);
        }
    }

    public ShaderPart LoadShaderPart(string sourcePath)
    {
        string source;
        bool sourceReadSucceeded;
        sourceReadSucceeded = ReadFile(sourcePath, out source);

        if (!ReadFile(sourcePath, out source))
            throw new Exception($"Cannot load shader source code. Path: {sourcePath}");

        ShaderType? type = Util.IdentifyShaderSource(sourcePath);

        if (type is null)
            throw new Exception($"Cannot infer shader type from code file extension. Path: {sourcePath}");

        try
        {
            ShaderPart shaderPart = new ShaderPart(source, type.Value);
            return shaderPart;
        }
        catch (Exception e)
        {
            throw new Exception(
                @$"Failed to compile shader source code in {sourcePath}
                Log:
                {e.Message}", e);
        }
    }
    #endregion

    #endregion

    #region Formatting
    string CompleteFilePath(string indepPath) => Path.Combine(basePath, PlatformFilePath(indepPath));
    string PlatformFilePath(string indepPath) => Path.Combine(indepPath.Split('/'));
    #endregion
}