using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

namespace GLAV.Helpers.Util;
public static class Util
{
    public static void LabelResource(GLResourceHandle handle, string name)
    {
        ObjectLabelIdentifier objLabelId = GLResourceTypeToLabelIdentifier(handle.resourceType);
        GL.ObjectLabel(objLabelId, handle.id, name.Length, name);
    }
    public static ObjectLabelIdentifier GLResourceTypeToLabelIdentifier(GLResourceType type)
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