using OpenTK.Graphics.OpenGL4;
using GLAV.Data;

namespace GLAV.Types;
public class VertexArray : GLResource
{
    public int VertexCount { get; protected set; }
    public VertexInfo VertexInfo { get; protected set; }
    Buffer vertexBuffer;
    public VertexArray()
    {
        Handle.resourceType = GLResourceType.VertexArray;
        Handle.id = GL.GenVertexArray();
    }
    public void Alloc<Vertex>(ref Vertex[] vertexArray, VertexInfo vertexInfo, BufferUsageHint usageHint) where Vertex : struct
    {
        vertexBuffer = new Buffer();
        vertexBuffer.Alloc(BufferTarget.ArrayBuffer, ref vertexArray, vertexArray.Length * vertexInfo.totalVertexSize, usageHint);

        GL.BindVertexArray(Handle.id); // Vertex buffer is still bound

        for (int i = 0; i < vertexInfo.attributes.Length; i++)
        {
            VertexAttributeData attrib = vertexInfo.attributes[i];
            GL.VertexAttribPointer(
                index: attrib.location,
                size: attrib.componentCount,
                type: attrib.type,
                normalized: false,
                stride: vertexInfo.totalVertexSize,
                offset: attrib.offset);
            GL.EnableVertexAttribArray(attrib.location);
        }
        GL.BindVertexArray(0);
        GLRegistry.Instance.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }
    public void Bind()
    {
        GL.BindVertexArray(Handle.id);
    }
    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteVertexArray(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteVertexArray(Handle.id));
    }
}