using System.Reflection;
using System.Runtime.CompilerServices;
using StbImageSharp;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

namespace Voxand.Content;
public class ContentManager(string basePath, string asmBasePath)
{
    readonly string basePath = basePath;
    readonly string asmBasePath = asmBasePath;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Stream OpenEmbedded(string path)
    {
        path = string.Join('.', asmBasePath, path);
        return Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
    }

    #region Basic reading
    public string ReadFile(string subpath, out bool succeeded)
    {
        string path = ExtendFilePath(basePath, subpath);
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
    public bool WriteFile(string subpath, string text)
    {
        string path = ExtendFilePath(basePath, subpath);
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
    #endregion
    public Texture2D LoadTexture(string path)
    {
        Texture2D texture = new Texture2D();
        using Stream stream = OpenEmbedded(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        texture.Alloc(new Vector2i(image.Width, image.Height), new TextureFormat(PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte), nint.Zero);
        return texture;
    }

    #region Formatting
    public static string ExtendFilePath(string basePath, string subpath)
    {
        string[] subs = subpath.Split('/');
        return Path.Combine(basePath, Path.Combine(subs));
    }
    public static string PlatformFilePath(string indepPath)
    {
        string[] subs = indepPath.Split('/');
        return Path.Combine(subs);
    }
    public static string ToEmbeddedResourcePath(string indepPath)
    {
        string[] subs = indepPath.Split('/');
        return string.Join('.', subs);
    }
    #endregion
}