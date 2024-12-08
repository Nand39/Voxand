using GLAV.Systems;
using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types;
public struct FramebufferAttachmentInfo(Texture2D texture, FramebufferAttachment attahcmentType, TextureTarget textureTarget)
{
    public Texture2D texture = texture;
    public FramebufferAttachment attahcmentType = attahcmentType;
    public TextureTarget textureTarget = textureTarget;
}

public class Framebuffer : GLResource
{
    public Framebuffer()
    {
        Handle.resourceType = GLResourceType.Framebuffer;
        Handle.id = GL.GenFramebuffer();
    }
    public bool Create(ref FramebufferAttachmentInfo[] attachments)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle.id);

        foreach (FramebufferAttachmentInfo attachment in attachments)
            Attach(attachment);
        
        FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            return false;

        return true;
    }
    public void BindFramebuffer(FramebufferTarget target)
    {
        GLRegistry.Instance.BindFramebuffer(target, Handle.id);
    }
    public void Attach(FramebufferAttachmentInfo attachment)
    {
        BindFramebuffer(FramebufferTarget.Framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment.attahcmentType, attachment.textureTarget, attachment.texture.Handle.id, 0);
    }
    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteFramebuffer(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteFramebuffer(Handle.id));
    }
}