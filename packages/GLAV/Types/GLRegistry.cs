using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Helpers.ExtensionMethods;
using GLAV.Types;
using static System.Net.Mime.MediaTypeNames;

namespace GLAV.Systems;
public static class GLRegistry
{
    static Texture2D[] textureUnits = new Texture2D[GL.GetInteger(GetPName.MaxCombinedTextureImageUnits)];
    static int activeTextureUnit;

    static Texture2D[] imageUnits = new Texture2D[GL.GetInteger(GetPName.MaxCombinedImageUniforms)];

    static int[] bufferTargets;
    static Dictionary<BufferTarget, int> bufferTargetMapping;

    static int activeShaderProgram = -1;
    static int activeFramebuffer = 0;

    static GLRegistry()
    {
        BufferTarget[] bufferTargets = (BufferTarget[])Enum.GetValues(typeof(BufferTarget));
        GLRegistry.bufferTargets = new int[bufferTargets.Length];
        bufferTargetMapping = [];
        for (int i = 0; i < bufferTargets.Length; i++)
        {
            GLRegistry.bufferTargets[i] = -1;
            bufferTargetMapping[bufferTargets[i]] = i;
        }
    }

    #region TEXTURES

    #region TEX BINDING
    public static void SelectTextureUnit(int unit)
    {
        if (activeTextureUnit == unit) return;
        activeTextureUnit = unit;
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
    }

    public static void BindTexture(Texture2D tex, TextureTarget target)
    {
        Texture2D currentlyBound = textureUnits[activeTextureUnit];
        if (currentlyBound is not null)
        {
            if (currentlyBound == tex)
                return;
            currentlyBound.MarkTextureUnbound();
        }
        textureUnits[activeTextureUnit] = tex;
        GL.BindTexture(target, tex.Handle.id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindTexture(Texture2D tex, int unit, TextureTarget target)
    {
        SelectTextureUnit(unit);
        BindTexture(tex, target);
    }

    public static void BindTextureRaw(int handle, int unit, TextureTarget target)
    {
        SelectTextureUnit(unit);
        BindTextureRaw(handle, target);
    }

    public static void BindTextureRaw(int handle, TextureTarget target)
    {
        Texture2D currentlyBound = textureUnits[activeTextureUnit];
        if (currentlyBound is not null)
        {
            if (currentlyBound.Handle.id == handle)
                return;
            currentlyBound.MarkTextureUnbound();
        }
        textureUnits[activeTextureUnit] = null;
        GL.BindTexture(target, handle);
    }
    #endregion
    
    #region IMG BINDING
    public static void BindImage(Texture2D tex, int binding, TextureAccess access, SizedInternalFormat format)
    {
        Texture2D currentlyBound = imageUnits[binding];
        if (currentlyBound != null)
            currentlyBound.MarkImageUnbound();
        imageUnits[binding] = tex;
        GL.BindImageTexture(binding, tex.Handle.id, 0, false, 0, access, format);
    }
    #endregion

    #endregion

    #region FRAMEBUFFERS
    public static void BindFramebuffer(FramebufferTarget target, int handle)
    {
        if (activeFramebuffer == handle)
            return;
        activeFramebuffer = handle;
        GL.BindFramebuffer(target, handle);
    }
    #endregion

    #region BUFFERS
    public static void BindBuffer(BufferTarget target, int handle)
    {
        int targetIndex = bufferTargetMapping[target];
        if (bufferTargets[targetIndex] == handle) 
            return;
        bufferTargets.ReplaceFirst(handle, -1);
        bufferTargets[targetIndex] = handle;
        GL.BindBuffer(target, handle);
    }
    #endregion

    #region SHADERS
    public static void UseProgram(int handle)
    {
        if (activeShaderProgram == handle)
            return;
        activeShaderProgram = handle;
        GL.UseProgram(handle);
    }
    public static void DeleteProgram(int handle)
    {
        if (activeShaderProgram == handle)
            activeShaderProgram = -1;
        GL.DeleteProgram(handle);
    }
    #endregion
}