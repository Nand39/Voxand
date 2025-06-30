using OpenTK.Graphics.OpenGL4;

using GLAV.Data;

namespace GLAV.Types;
public class VertexArray : GLResource
{
    public Buffer? ReferencedElementBuffer { get; internal set; } = null;

    public VertexArray()
    {
        Handle.resourceType = GLResourceType.VertexArray;
        Handle.id = GL.GenVertexArray();
        Bind();
    }

    public void Bind()
    {
        GLRegistry.Instance.BindVertexArray(this);
    }

    public static void Unbind() => GLRegistry.Instance.UnbindVertexArray();

    public void SetVertexAttributePointer(VertexAttributeInfo attribInfo, Buffer source)
    {
        source.Bind(BufferTarget.ArrayBuffer);
        Bind();
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