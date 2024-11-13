using OpenTK.Graphics.OpenGL4;

using DisposableExt;

using GLAV.Systems;
using GLAV.Data;

namespace GLAV.Types;
public class VertexArray : GLResource
{
    public VertexArray()
    {
        Handle.resourceType = GLResourceType.VertexArray;
        Handle.id = GL.GenVertexArray();
    }
    public void Alloc<Vertex>(ref Vertex[] vertexArray, VertexInfo vertexInfo, BufferUsageHint usageHint) where Vertex : struct
    {
        Buffer vertexBuffer = new Buffer();
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
        vertexBuffer.Dispose();
        GLRegistry.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }
    public void Bind()
    {
        GL.BindVertexArray(Handle.id);
    }
    public override void Free()
    {
        GL.DeleteVertexArray(Handle.id);
    }
}