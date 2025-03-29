using OpenTK.Graphics.OpenGL4;

namespace GLAV.Data;
public struct VertexAttributeInfo(int location, int componentCount, int offset, VertexAttribPointerType type, bool normalized, int stride)
{
    public int location = location;
    public int componentCount = componentCount;
    public int offset = offset;
    public VertexAttribPointerType type = type;
    public bool normalized = normalized;
    public int stride = stride;
}

[AttributeUsage(AttributeTargets.Field)]
public class VertexAttribAttribute(int location, bool normalized = false) : Attribute
{
    public int Location { get; } = location;
    public bool Normalized { get; } = normalized;
}