using OpenTK.Graphics.OpenGL4;
using System.Net.Mail;

namespace GLAV.Types;
public struct FramebufferAttachmentInfo(Texture2D texture, FramebufferAttachment attahcmentType, TextureTarget textureTarget)
{
    public Texture2D texture = texture;
    public FramebufferAttachment attachmentType = attahcmentType;
    public TextureTarget textureTarget = textureTarget;
}

public class Framebuffer : GLResource
{
    public Framebuffer()
    {
        Handle.resourceType = GLResourceType.Framebuffer;
        Handle.id = GL.GenFramebuffer();
        Bind(FramebufferTarget.Framebuffer);
    }
    public void Create(ref FramebufferAttachmentInfo[] attachments)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle.id);

        foreach (FramebufferAttachmentInfo attachment in attachments)
            Attach(attachment);

        FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Framebuffer error code: " + status);
    }
    public void Bind(FramebufferTarget target)
    {
        GLRegistry.Instance.BindFramebuffer(FramebufferTarget.Framebuffer, Handle.id);
    }
    public void Attach(FramebufferAttachmentInfo attachment)
    {
        Bind(FramebufferTarget.Framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment.attachmentType, attachment.textureTarget, attachment.texture.Handle.id, 0);
    }
    public void Detach(FramebufferAttachment attachmentType)
    {
        Bind(FramebufferTarget.Framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, TextureTarget.Texture2D, 0, 0);
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