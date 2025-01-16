using System.Reflection;
using System.Runtime.CompilerServices;
using StbImageSharp;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using Voxand.Helpers;

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


    #region Basic reading/writing text
    public bool ReadFile(string path, out string result)
    {
        path = CompleteFilePath(path);
        try
        {
            using StreamReader reader = new(path);
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
    public Texture2D LoadTexture(string path)
    {
        Stream stream;
        stream = OpenStream(path, FileMode.Open, FileAccess.Read);

        string? extension = Path.GetExtension(path);
        if (extension is null or "")
            throw new ArgumentException($"Cannot load texture at {path}; no extension found.");

        Texture2D texture;
        switch (extension)
        {
            default: throw new ArgumentException
                    ($"Cannot load image with extesion {extension}; supported extensions: .png, .jpg, .bmp, .tga, .hdr");
            
            case ".png":
            case ".jpg":
            case ".bmp":
            case ".tga":
                texture = LoadSimpleTexture(stream); break;

            case ".hdr": 
                texture = LoadHDRTexture(stream); break;
        }
        
        texture.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
        texture.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest));

        stream.Dispose();
        return texture;
    }
    Texture2D LoadSimpleTexture(Stream imageFileStream)
    {
        ImageResult image = ImageResult.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        byte[] data = image.Data;
        Texture2D texture = new Texture2D();
        texture.Alloc(new Vector2i(image.Width, image.Height), new(PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte), data);
        return texture;
    }
    Texture2D LoadHDRTexture(Stream imageFileStream)
    {
        ImageResultFloat image = ImageResultFloat.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        float[] data = image.Data;
        Texture2D texture = new Texture2D();
        texture.Alloc(new Vector2i(image.Width, image.Height), new(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float), data);
        return texture;
    }
    public Shader LoadShader(params string[] sourcePaths)
    {
        ShaderPart[] shaderAttachments = new ShaderPart[sourcePaths.Length];
        for (int i = 0; i < shaderAttachments.Length; i++)
            shaderAttachments[i] = LoadShaderPart(sourcePaths[i]);

        Shader shader = new();
        bool succeeded = shader.Create(shaderAttachments);

        if (!succeeded)
            throw new Exception(
@$"Cannot load shader.
Log: {GL.GetProgramInfoLog(shader.Handle.id)}
Parts: {string.Join(";\n", sourcePaths)}");

        return shader;
    }
    public ShaderPart LoadShaderPart(string sourcePath)
    {
        string source;
        bool succeeded;
        succeeded = ReadFile(sourcePath, out source);

        if (!ReadFile(sourcePath, out source))
            throw new Exception($"Cannot load shader source code. Path: {sourcePath}");

        ShaderType? type = Util.IdentifyShaderSource(sourcePath);

        if (type is null)
            throw new Exception($"Cannot infer shader type from code file extension. Path: {sourcePath}");

        ShaderPart shaderPart = new ShaderPart(source, type.Value);
        shaderPart.Lable = "Unnamed shader part";

        return shaderPart;
    }

    #endregion

    #region Formatting
    string CompleteFilePath(string indepPath) => Path.Combine(basePath, PlatformFilePath(indepPath));
    string PlatformFilePath(string indepPath) => Path.Combine(indepPath.Split('/'));
    #endregion
}