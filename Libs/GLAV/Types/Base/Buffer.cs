using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using System.Text;

namespace GLAV.Types;
public readonly struct BufferBindingInfo(BufferRangeTarget target, int index)
{
    public readonly BufferRangeTarget target = target;
    public readonly int index = index;
}

public class Buffer : GLResource
{
    public int Size { get; protected set; } = 0;
    public BufferTarget bufferTarget { get; protected set; }
    public BufferBindingInfo bindingInfo { get; protected set; }
    public Buffer()
    {
        Handle.resourceType = GLResourceType.Buffer;
        Handle.id = GL.GenBuffer();
        Bind(BufferTarget.ShaderStorageBuffer);
    }

    public void Alloc(BufferTarget bufferTarget, int size, BufferUsageHint usageHint)
    {
        Size = size;
        Bind(bufferTarget);
        GL.BufferData(bufferTarget, size, nint.Zero, usageHint);
    }
    public void Alloc<T>(BufferTarget bufferTarget, T data, int size, BufferUsageHint usageHint) 
        where T : struct
    {
        Size = size;
        Bind(bufferTarget);
        GL.BufferData(bufferTarget, size, ref data, usageHint);
    }
    public unsafe void Alloc<T>(BufferTarget bufferTarget, T[] data, int size, BufferUsageHint usageHint) 
        where T : struct
    {
        Size = size;
        Bind(bufferTarget);
        GL.BufferData(bufferTarget, size, data, usageHint);
    }
    public unsafe void Alloc<T>(BufferTarget bufferTarget, Span<T> data, int size, BufferUsageHint usageHint)
        where T : struct
    {
        Size = size;
        Bind(bufferTarget);
        fixed (T* startPtr = data)
            GL.BufferData(bufferTarget, size, (nint)startPtr, usageHint);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind() => GLRegistry.Instance.BindBuffer(bufferTarget, Handle.id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(BufferTarget target) 
    {
        bufferTarget = target;
        Bind();
    }

    #region Store
    public unsafe void Store(int offsetInBytes, nint dataPtr, int size)
    {
        Bind();
        GL.BufferSubData(bufferTarget, offsetInBytes, size, dataPtr);
    }
    public unsafe void Store<T>(ref T data, int offsetInBytes)
        where T : struct
    {
        Bind();
        GL.BufferSubData(bufferTarget, offsetInBytes, sizeof(T), ref data);
    }
    public unsafe void Store<T>(T[] data, int readingOffsetInBytes, int sizeInBytes, int writingOffsetInBytes)
        where T : struct
    {
        Bind();
        fixed (T* startPtr = data)
            GL.BufferSubData(bufferTarget, writingOffsetInBytes, sizeInBytes, (nint)startPtr + readingOffsetInBytes);
    }
    public unsafe void Store<T>(Span<T> data, int writingOffsetInBytes)
        where T : struct
    {
        Bind();
        fixed (T* startPtr = data)
            GL.BufferSubData(bufferTarget, writingOffsetInBytes, data.Length * sizeof(T), (nint)startPtr);
    }
    #endregion

    public unsafe void LogContent<Format>() where Format : struct
    {
        int itemSize = sizeof(Format);
        int count = Size / itemSize;
        string contentLog = GetContentLog<Format>(0, count);
        Console.WriteLine(contentLog);

        int bytesOmitted = Size - (count * itemSize);
        if (bytesOmitted > 0)
            Console.WriteLine($"{bytesOmitted} bytes omitted!");
    }
    public string GetContentLog<Format>(int startOffsetInBytes, int count) where Format : struct
    {
        int itemSize; unsafe { itemSize = sizeof(Format); }
        Format[] data = Retrieve<Format>(startOffsetInBytes, count);

        StringBuilder stringBuilder = new();
        stringBuilder.Append($"Content of {Lable} from {startOffsetInBytes} to {startOffsetInBytes + count * itemSize} ({count} items)\n");
        for (int i = 0; i < data.Length; i++)
        {
            stringBuilder.Append($"{data[i]}\n");
        }
        stringBuilder.Append("End of buffer content log.");
        return stringBuilder.ToString();
    }
    public void CopyTo(Buffer destinationBuffer, int readingOffsetInBytes, int writingOffsetInBytes, int size)
    {
        BufferTarget sourceOriginalTarget = bufferTarget;
        BufferTarget destinationOriginalTarget = destinationBuffer.bufferTarget;

        Bind(BufferTarget.CopyReadBuffer);
        destinationBuffer.Bind(BufferTarget.CopyWriteBuffer);

        GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, readingOffsetInBytes, writingOffsetInBytes, size);

        Bind(sourceOriginalTarget);
        destinationBuffer.Bind(destinationOriginalTarget);
    }

    public unsafe T[] Retrieve<T>(int readingOffsetInBytes, int count) where T : struct
    {
        Bind();
        int size = sizeof(T) * count;
        T[] output = new T[count];
        fixed (T* outputPtr = output)
        {
            GL.GetBufferSubData(bufferTarget, readingOffsetInBytes, size, (nint)outputPtr);
        }
        return output;
    }
    public unsafe T Retrieve<T>(int readingOffsetInBytes) where T : struct
    {
        int size = sizeof(T);
        if (readingOffsetInBytes + size > Size)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(readingOffsetInBytes),
                actualValue: readingOffsetInBytes,
                message: $"{nameof(readingOffsetInBytes)} + size of the type to retrieve ({typeof(T)}; size = {size}) " +
                         $"should not exceed buffer bounds. Tried accessing bytes from {readingOffsetInBytes} to {readingOffsetInBytes + size} out of {Size}.");

        T output = new T();
        Bind();
        GL.GetBufferSubData(bufferTarget, readingOffsetInBytes, size, (nint)(&output));
        return output;
    }
    public unsafe T Retrieve<T>(int y, int z, int x, int dimX, int dimZ) where T : struct
    {
        int index = y * dimZ * dimX + z * dimX + x;
        return Retrieve<T>(index * sizeof(T));
    }

    public void BindAsShaderStorage(BufferBindingInfo bindingInfo)
    {
        this.bindingInfo = bindingInfo;
        GL.BindBufferBase(bindingInfo.target, bindingInfo.index, Handle.id);
    }

    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteBuffer(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteBuffer(Handle.id));
    }
}