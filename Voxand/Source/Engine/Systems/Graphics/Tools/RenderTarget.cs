using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Types;
using GLAV;

using Voxand.Helpers;
using DisposableExt;

namespace Voxand.Engine.Systems.Graphics.Tools;
public abstract class RenderTarget
{
    sealed class DefaultRenderTarget : RenderTarget
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
        public override void Use()
        {
            GLRegistry.Instance.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Util.ClientSize.X, Util.ClientSize.Y);
        }
    }
    public static RenderTarget Default => DefaultRenderTarget.Instance;
    public abstract void Use();
}
public class TexturedRenderTarget : RenderTarget, IDisposableExt
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
    /// when this <see cref="TexturedRenderTarget"/> is used.
    /// </param>
    public TexturedRenderTarget(Vector2i resolution, params FramebufferAttachmentInfo[] attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        Resolution = resolution;
        framebuffer = new();
        framebuffer.Create(ref attachments);
        DisposeHelper = new(this);
    }
    public override void Use()
    {
        framebuffer.Bind(FramebufferTarget.Framebuffer);
        GL.Viewport(0, 0, Resolution.X, Resolution.Y);
    }
    void IDisposableExt.Free() => framebuffer.Dispose();
    ~TexturedRenderTarget() => this.Dispose();
}