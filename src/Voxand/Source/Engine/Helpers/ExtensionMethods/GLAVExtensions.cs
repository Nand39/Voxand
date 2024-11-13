using GLAV.Types;
using OpenTK.Graphics.OpenGL4;
using Voxand.Content;

namespace Voxand.Helpers.ExtensionMethods.GLAVExtensions;
public static class GLAVExtensions
{
    public static void Create(this Shader shader, ContentManager content, bool embedded, params string[] sourcePaths)
    {
        ShaderPart[] shaderAttachments = new ShaderPart[sourcePaths.Length];
        for (int i = 0; i < shaderAttachments.Length; i++)
            shaderAttachments[i] = Load(content, sourcePaths[i], embedded);

        shader.Create(shaderAttachments);
    } 
    public static ShaderPart Load(ContentManager content, string path, bool embedded)
    {
        string source = embedded ? content.ReadEmbedded(path, out bool status) : content.ReadFile(path, out status);

        if (!status)
            throw new Exception($"Cannot load shader source; path: {path}");

        ShaderType? type = Util.IdentifyShaderSource(path);

        if (type is null)
            throw new Exception($"Cannot identify shader source; path: {path}");

        return new ShaderPart(source, type.Value);
    }
}