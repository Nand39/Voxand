using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Types;
using GLAV.Systems;

using Voxand.Helpers;
using DisposableExt;

namespace Voxand.Engine.Graphics.Tools;
public interface IRenderTarget
{
    void Use();
}
public class RenderTarget : IDisposableExt, IRenderTarget
{
    public Framebuffer framebuffer;
    public Vector2i Resolution { get; set; }
    public DisposeHelper DisposeHelper { get; }

    /// <param name="resolution">
    /// This parameter will be used in <see cref="GL.Viewport(int, int, int, int)"/> 
    /// to map NDC from (0; 0) to <paramref name="resolution"/>.
    /// </param>
    /// <param name="attachments">
    /// Textures that will be used as destinations for rendering 
    /// when this <see cref="RenderTarget"/> is used.
    /// </param>
    public RenderTarget(Vector2i resolution, params FramebufferAttachmentInfo[] attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        Resolution = resolution;
        framebuffer = new();
        framebuffer.Create(ref attachments);
        DisposeHelper = new(this);
    }
    public void Use()
    {
        framebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);
        GL.Viewport(0, 0, Resolution.X, Resolution.Y);
    }
    void IDisposableExt.Free() => framebuffer.Dispose();
    ~RenderTarget() => this.Dispose();
}
public sealed class DefaultRenderTarget : IRenderTarget
{
    static DefaultRenderTarget instance;
    public static DefaultRenderTarget Instance
    {
        get
        {
            if (instance is null)
                instance = new();
            return instance;
        }
    }
    DefaultRenderTarget() { }
    public void Use()
    {
        GLRegistry.Instance.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, Util.ClientSize.X, Util.ClientSize.Y);
    }
}