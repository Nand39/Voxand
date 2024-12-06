using System.Reflection;
using System.Runtime.CompilerServices;
using StbImageSharp;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using Voxand.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;

namespace Voxand.Content;
public class ContentManager(string basePath, string asmBasePath)
{
    readonly string basePath = basePath;
    readonly string asmBasePath = asmBasePath;

    static ContentManager()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Stream OpenEmbedded(string path)
    {
        path = string.Join('.', asmBasePath, path);
        return Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
    }

    #region Basic text manipulations
    public string ReadFile(string path, bool embedded, out bool succeeded)
    {
        return embedded ? ReadEmbedded(path, out succeeded) : ReadFile(path, out succeeded);
    }
    public string ReadFile(string path, out bool succeeded)
    {
        path = ExtendFilePath(basePath, path);
        succeeded = true;
        try
        {
            using StreamReader reader = new(path);
            return reader.ReadToEnd();
        }
        catch
        {
            succeeded = false;
            return string.Empty;
        }
    }
    public string ReadEmbedded(string path, out bool succeeded)
    {
        using Stream stream = OpenEmbedded(path);
        succeeded = true;
        try 
        {
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
        catch 
        {
            succeeded = false;
            return string.Empty;
        }
    }
    public bool WriteFile(string path, string text)
    {
        path = ExtendFilePath(basePath, path);
        try
        {
            using FileStream stream = new(path, FileMode.OpenOrCreate);
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
    public Texture2D LoadTexture(string path, bool embedded)
    {
        Stream stream;
        if (embedded)
        {
            stream = OpenEmbedded(ToEmbeddedResourcePath(path));
        }
        else
        {
            stream = new FileStream(ExtendFilePath(basePath, path), FileMode.Open);
        }

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
        texture.Alloc(new Vector2i(image.Width, image.Height), new(PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte), ref data);
        return texture;
    }
    Texture2D LoadHDRTexture(Stream imageFileStream)
    {
        ImageResultFloat image = ImageResultFloat.FromStream(imageFileStream, ColorComponents.RedGreenBlueAlpha);
        float[] data = image.Data;
        Texture2D texture = new Texture2D();
        texture.Alloc(new Vector2i(image.Width, image.Height), new(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float), ref data);
        return texture;
    }
    public Shader LoadShader(bool embedded, params string[] sourcePaths)
    {
        ShaderPart[] shaderAttachments = new ShaderPart[sourcePaths.Length];
        for (int i = 0; i < shaderAttachments.Length; i++)
            shaderAttachments[i] = LoadShaderPart(sourcePaths[i], embedded);

        Shader shader = new();
        bool succeeded = shader.Create(shaderAttachments);

        if (!succeeded)
            throw new Exception($"Cannot load shader; parts: {string.Join("; ", sourcePaths)}");

        return shader;
    }
    public ShaderPart LoadShaderPart(string sourcePath, bool embedded)
    {
        string source = embedded ? ReadEmbedded(sourcePath, out bool succeeded) : ReadFile(sourcePath, out succeeded);

        if (!succeeded)
            throw new Exception($"Cannot load shader source; path: {sourcePath}");

        ShaderType? type = Util.IdentifyShaderSource(sourcePath);

        if (type is null)
            throw new Exception($"Cannot identify shader source; path: {sourcePath}");

        ShaderPart shaderPart = new ShaderPart(source, type.Value);
        shaderPart.Lable = "Unnamed shader part";

        return shaderPart;
    }

    #endregion

    #region Formatting
    public static string ExtendFilePath(string basePath, string subpath)
    {
        string[] subDirs = subpath.Split('/');
        return Path.Combine(basePath, Path.Combine(subDirs));
    }
    public static string PlatformFilePath(string indepPath)
    {
        string[] dirs = indepPath.Split('/');
        return Path.Combine(dirs);
    }
    public static string ToEmbeddedResourcePath(string indepPath)
    {
        string[] subs = indepPath.Split('/');
        return string.Join('.', subs);
    }
    #endregion
}