using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;
using Buffer = GLAV.Types.Buffer;

namespace GLAV;
public sealed class GLRegistry
{
    static GLRegistry instance;
    public static GLRegistry Instance => instance;

    Texture2D[] textureUnits = new Texture2D[GL.GetInteger(GetPName.MaxCombinedTextureImageUnits)];
    int activeTextureUnit;

    Texture2D[] imageUnits = new Texture2D[GL.GetInteger(GetPName.MaxCombinedImageUniforms)];

    int[] bufferTargets;
    Dictionary<BufferTarget, int> bufferTargetMapping;

    Buffer[] UniformBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxUniformBufferBindings)];
    Buffer[] TransformFeedbackBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxTransformFeedbackBuffers)];
    Buffer[] ShaderStorageBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxShaderStorageBufferBindings)];
    Buffer[] AtomicCounterBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxAtomicCounterBufferBindings)];

    int activeShaderProgram = -1;
    int activeFramebuffer = 0;

    ConcurrentQueue<Action> pendingGLActions = new();

    public IGLFWGraphicsContext GLFWGraphicsContext { get; private set; }

    GLRegistry() { }
    public static void Initialize(IGLFWGraphicsContext context)
    {
        instance = new();
        instance.GLFWGraphicsContext = context;
        BufferTarget[] bufferTargetEnums = (BufferTarget[])Enum.GetValues(typeof(BufferTarget));
        instance.bufferTargets = new int[bufferTargetEnums.Length];
        instance.bufferTargetMapping = [];
        for (int i = 0; i < bufferTargetEnums.Length; i++)
        {
            instance.bufferTargets[i] = -1;
            instance.bufferTargetMapping[bufferTargetEnums[i]] = i;
        }
    }
    public void ProcessOpenGLActions()
    {
        if (!GLFWGraphicsContext.IsCurrent)
            throw new InvalidOperationException("Cannot process OpenGL actions on a thread that does not have OpenGL context.");

        while (pendingGLActions.TryDequeue(out Action? action))
            action?.Invoke();
    }
    public void ScheduleAction(Action action) => pendingGLActions.Enqueue(action);

    #region TEXTURES

    #region TEX BINDING
    public void SelectTextureUnit(int unit)
    {
        if (activeTextureUnit == unit) return;
        activeTextureUnit = unit;
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
    }

    public void BindTexture(Texture2D tex, TextureTarget target)
    {
        Texture2D currentlyBound = textureUnits[activeTextureUnit];
        if (currentlyBound == tex)
            return;
        textureUnits[activeTextureUnit] = tex;
        GL.BindTexture(target, tex.Handle.id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(Texture2D tex, int unit, TextureTarget target)
    {
        SelectTextureUnit(unit);
        BindTexture(tex, target);
    }

    public void BindTextureRaw(int handle, int unit, TextureTarget target)
    {
        SelectTextureUnit(unit);
        BindTextureRaw(handle, target);
    }

    public void BindTextureRaw(int handle, TextureTarget target)
    {
        Texture2D currentlyBound = textureUnits[activeTextureUnit];
        if (currentlyBound is not null && currentlyBound.Handle.id == handle)
            return;

        textureUnits[activeTextureUnit] = null;
        GL.BindTexture(target, handle);
    }
    #endregion
    
    #region IMG BINDING
    public void BindImage(Texture2D tex, int binding, TextureAccess access, SizedInternalFormat format)
    {
        imageUnits[binding] = tex;
        GL.BindImageTexture(binding, tex.Handle.id, 0, false, 0, access, format);
    }
    #endregion

    #endregion

    #region BUFFERS
    public void BindBuffer(BufferTarget target, int handle)
    {
        int targetIndex = bufferTargetMapping[target];
        if (bufferTargets[targetIndex] == handle) 
            return;
        bufferTargets[targetIndex] = handle;
        GL.BindBuffer(target, handle);
    }

    public void BindBufferAsShaderStorage(BufferRangeTarget target, Buffer buffer, BufferBindingInfo binding)
    {
        if (binding.offset == -1)
            GL.BindBufferBase(target, binding.binding, buffer.Handle.id);
        else
            GL.BindBufferRange(target, binding.binding, buffer.Handle.id, binding.offset, binding.size);

        switch (target)
        {
            case BufferRangeTarget.UniformBuffer:
                {
                    UniformBufferBindings[binding.binding]?.uniformBufferBindings.Remove(binding.binding);
                    UniformBufferBindings[binding.binding] = buffer;
                    break;
                }
            case BufferRangeTarget.TransformFeedbackBuffer:
                {
                    TransformFeedbackBufferBindings[binding.binding]?.transformFeedbackBufferBindings.Remove(binding.binding);
                    TransformFeedbackBufferBindings[binding.binding] = buffer;
                    break;
                }
            case BufferRangeTarget.ShaderStorageBuffer:
                {
                    ShaderStorageBufferBindings[binding.binding]?.shaderStorageBufferBindings.Remove(binding.binding);
                    ShaderStorageBufferBindings[binding.binding] = buffer;
                    break;
                }
            case BufferRangeTarget.AtomicCounterBuffer:
                {
                    AtomicCounterBufferBindings[binding.binding]?.atomicCounterBufferBindings.Remove(binding.binding);
                    AtomicCounterBufferBindings[binding.binding] = buffer;
                    break;
                }
        }
    }

    #endregion

    #region FRAMEBUFFERS
    public void BindFramebuffer(FramebufferTarget target, int handle)
    {
        if (activeFramebuffer == handle)
            return;
        activeFramebuffer = handle;
        GL.BindFramebuffer(target, handle);
    }
    #endregion

    #region SHADERS
    public void UseProgram(int handle)
    {
        if (activeShaderProgram == handle)
            return;
        activeShaderProgram = handle;
        GL.UseProgram(handle);
    }
    public void DeleteProgram(int handle)
    {
        if (activeShaderProgram == handle)
            activeShaderProgram = -1;
        GL.DeleteProgram(handle);
    }
    #endregion
}