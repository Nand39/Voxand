using OpenTK.Graphics.OpenGL4;

using DisposableExt;

using GLAV.Helpers.Util;

namespace GLAV.Types;
public abstract class GLResource : IDisposableExt
{
    public GLResourceHandle Handle = new();
    public DisposeHelper DisposeHelper { get; }
    public GLResource() => DisposeHelper = new(this); 
    public string GLLable
    {
        get
        {
            GL.GetObjectLabel(Util.GLResourceTypeToLabelIdentifier(Handle.resourceType), Handle.id, 200, out int length, out string label);
            if (label == "") label = "UnnamedResource";
            return label;
        }
        set => Util.LabelResource(Handle, value);
    }
    public string Lable { get; set; } = "UnnamedResource";
    void IDisposableExt.Free() => Free();
    protected abstract void Free();
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
    Texture2D,
    Framebuffer,
}