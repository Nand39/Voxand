using OpenTK.Graphics.OpenGL4;

using DisposableExt;

using GLAV.Helpers.Internal;
using System.Text;

namespace GLAV.Types;
public abstract class GLResource : IDisposableExt
{
    public GLResourceHandle Handle = new();
    public DisposeState DisposeState { get; }
    public GLResource() => DisposeState = new(this); 
    public string Label
    {
        get
        {
            GL.GetObjectLabel(
                identifier: Util.GetLabelIdentifier(Handle.resourceType),
                name: Handle.id,
                bufSize: Encoding.UTF8.GetMaxByteCount(GL.GetInteger(GetPName.MaxLabelLength)),
                length: out int length,
                label: out string label);
            return label == string.Empty ? "UnnamedResource" : label;
        }
        set => Util.LabelResource(Handle, value);
    }
    public override string ToString() => $"\"{Label}\" (resource={Handle.resourceType}; id={Handle.id})";
    void IDisposableExt.Free() => Free(GLRegistry.Instance.GLFWGraphicsContext.IsCurrent);
    protected abstract void Free(bool hasContext);
    ~GLResource() => this.Dispose();
}
public struct GLResourceHandle(int handle, GLResourceType resourceType)
{
    public int id = handle;
    public GLResourceType resourceType = resourceType;
}
public enum GLResourceType
{
    Buffer,
    VertexArray,
    ShaderPart,
    ShaderProgram,
    Texture,
    Framebuffer,
}