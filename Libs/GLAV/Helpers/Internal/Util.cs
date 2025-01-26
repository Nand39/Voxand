using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using System.Diagnostics;

namespace GLAV.Helpers.Internal;
internal static class Util
{
    public static void LabelResource(GLResourceHandle handle, string label)
    {
        ObjectLabelIdentifier objLabelId = GetLabelIdentifier(handle.resourceType);
        int maxLength = GL.GetInteger(GetPName.MaxLabelLength);

        if (label.Length > maxLength)
            throw new ArgumentException($"Lable string should not exceed character limit (currently {maxLength})");

        GL.ObjectLabel(objLabelId, handle.id, label.Length, label);
    }
    public static ObjectLabelIdentifier GetLabelIdentifier(GLResourceType type)
    {
        switch (type)
        {
            default: return ObjectLabelIdentifier.Buffer;
            case GLResourceType.Buffer: return ObjectLabelIdentifier.Buffer;
            case GLResourceType.Texture2D: return ObjectLabelIdentifier.Texture;
            case GLResourceType.VertexArray: return ObjectLabelIdentifier.VertexArray;
            case GLResourceType.ShaderPart: return ObjectLabelIdentifier.Shader;
            case GLResourceType.ShaderProgram: return ObjectLabelIdentifier.Program;
            case GLResourceType.Framebuffer: return ObjectLabelIdentifier.Framebuffer;
        }
    }
}