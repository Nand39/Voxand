using OpenTK.Graphics.OpenGL4;

namespace GLAV.Data;
public class VertexInfo
{
    public readonly int totalVertexSize;
    public readonly VertexAttributeData[] attributes;
    public VertexInfo(VertexAttributeData[] attribs)
    {
        totalVertexSize = 0;
        attributes = attribs;
        for (int i = 0; i < attribs.Length; i++)
            totalVertexSize += attribs[i].componentCount * sizeof(float);
    }
}
public struct VertexAttributeData(int location, byte componentCount, byte offset, VertexAttribPointerType type)
{
    public int location = location;
    public byte componentCount = componentCount;
    public byte offset = offset;
    public VertexAttribPointerType type = type;
}