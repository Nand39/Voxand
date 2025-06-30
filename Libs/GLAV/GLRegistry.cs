using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;
using Buffer = GLAV.Types.Buffer;
using GLAV.Helpers.Internal;
using System.Reflection.Metadata;

namespace GLAV;
public sealed class GLRegistry
{
    static GLRegistry instance;
    public static GLRegistry Instance => instance;

    Texture[] textureUnits = new Texture[GL.GetInteger(GetPName.MaxCombinedTextureImageUnits)];
    int activeTextureUnit;

    Texture[] imageUnits = new Texture[GL.GetInteger(GetPName.MaxCombinedImageUniforms)];

    Buffer?[] bufferTargets;
    Dictionary<BufferTarget, int> bufferTargetMapping;

    Buffer[] UniformBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxUniformBufferBindings)];
    Buffer[] TransformFeedbackBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxTransformFeedbackBuffers)];
    Buffer[] ShaderStorageBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxShaderStorageBufferBindings)];
    Buffer[] AtomicCounterBufferBindings = new Buffer[GL.GetInteger((GetPName)All.MaxAtomicCounterBufferBindings)];

    int activeShaderProgram = -1;
    int activeFramebuffer = 0;
    VertexArray? activeVertexArray;

    ConcurrentQueue<Action> pendingGLActions = new();

    public IGLFWGraphicsContext GLFWGraphicsContext { get; private set; }

    GLRegistry() { }
    public static void Initialize(IGLFWGraphicsContext context)
    {
        instance = new();
        instance.GLFWGraphicsContext = context;
        BufferTarget[] bufferTargetEnums = (BufferTarget[])Enum.GetValues(typeof(BufferTarget));
        instance.bufferTargets = new Buffer[bufferTargetEnums.Length];
        instance.bufferTargetMapping = [];
        for (int i = 0; i < bufferTargetEnums.Length; i++)
        {
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

    public void BindTexture(Texture tex, TextureTarget target)
    {
        Texture currentlyBound = textureUnits[activeTextureUnit];
        if (currentlyBound == tex)
            return;
        textureUnits[activeTextureUnit] = tex;
        GL.BindTexture(target, tex.Handle.id);
    }

    public void BindTexture(Texture tex, int unit, TextureTarget target)
    {
        SelectTextureUnit(unit);
        BindTexture(tex, target);
    }
    #endregion
    
    #region IMG BINDING
    public void BindImage(Texture tex, int binding, TextureAccess access, SizedInternalFormat format)
    {
        imageUnits[binding] = tex;
        GL.BindImageTexture(binding, tex.Handle.id, 0, false, 0, access, format);
    }
    #endregion

    #endregion

    #region BUFFERS
    public void BindBuffer(BufferTarget target, Buffer? buffer)
    {
        int targetIndex = bufferTargetMapping[target];
        
        if (target == BufferTarget.ElementArrayBuffer && activeVertexArray is not null)
            activeVertexArray.ReferencedElementBuffer = buffer;

        Buffer? oldBuffer = bufferTargets[targetIndex];

        if (oldBuffer == buffer)
            return;

        if (oldBuffer is not null)
            oldBuffer.PointingBufferTargetFlags &= ~Util.TargetToFlag(target);

        bufferTargets[targetIndex] = buffer;

        GL.BindBuffer(target, buffer is null ? 0 : buffer.Handle.id);
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

    public void DeleteBuffer(Buffer buffer)
    {
        int pointingBufferTargets = (int)buffer.PointingBufferTargetFlags;
        for (int i = 0; i < bufferTargets.Length; i++)
            if ((pointingBufferTargets | (1 << i)) != 0)
                bufferTargets[i] = null;

        GL.DeleteBuffer(buffer.Handle.id);
    }

    #endregion

    #region VERTEX ARRAYS
    public void BindVertexArray(VertexArray vertexArray)
    {
        activeVertexArray = vertexArray;
        BindBuffer(BufferTarget.ElementArrayBuffer, vertexArray.ReferencedElementBuffer);
        GL.GetInteger(GetPName.ElementArrayBufferBinding, out int binding);
        GL.BindVertexArray(vertexArray.Handle.id);
        GL.GetInteger(GetPName.ElementArrayBufferBinding, out binding);
    }
    public void UnbindVertexArray()
    {
        activeVertexArray = null;
        GL.BindVertexArray(0);
        BindBuffer(BufferTarget.ElementArrayBuffer, null);
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