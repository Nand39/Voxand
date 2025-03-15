using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Data;
using GLAV.Helpers.Internal;

namespace GLAV.Types;
public class VertexArray : GLResource
{
    public VertexArray()
    {
        Handle.resourceType = GLResourceType.VertexArray;
        Handle.id = GL.GenVertexArray();
        Bind();
    }

    public void Bind()
    {
        GL.BindVertexArray(Handle.id);
    }

    public void SetVertexAttributePointer(VertexAttributeInfo attribInfo, Buffer source)
    {
        source.Bind(BufferTarget.ArrayBuffer);
        GL.VertexAttribPointer(
            index: attribInfo.location,
            size: attribInfo.componentCount,
            type: attribInfo.type,
            normalized: attribInfo.normalized,
            stride: attribInfo.stride,
            offset: attribInfo.offset);
        GL.EnableVertexAttribArray(attribInfo.location);
    }

    public void EnableVertexAttribute(int location) 
    {
        Bind();
        GL.EnableVertexAttribArray(location); 
    }
    public void DisableVertexAttribute(int location)
    {
        Bind();
        GL.DisableVertexAttribArray(location);
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