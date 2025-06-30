using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using GLAV.Data;
using GLAV.Helpers.Internal;
using System.Reflection;

namespace GLAV.Types.Extended;
public class VertexSpecification : IDisposableExt
{
    VertexArray vao;

    public DisposeHelper DisposeHelper { get; }

    public VertexSpecification()
    {
        DisposeHelper = new(this);
        vao = new();
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public string Label
    {
        get => vao.Label;
        set => vao.Label = value;
    }

    public unsafe void AddAttributeSource<VertexType>(Buffer source) where VertexType : struct
    {
        foreach (VertexAttributeInfo attribInfo in GenerateVertexInfo<VertexType>())
            vao.SetVertexAttributePointer(attribInfo, source);
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public unsafe void AddAttributeSource<VertexType>(TypedArray<VertexType> source) where VertexType : struct
    {
        foreach (VertexAttributeInfo attribInfo in GenerateVertexInfo<VertexType>())
            vao.SetVertexAttributePointer(attribInfo, source.Buffer);
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public void SetElementBuffer(Buffer? elementBuffer)
    {
        vao.Bind();

        if (elementBuffer is not null)
            elementBuffer.Bind(BufferTarget.ElementArrayBuffer);
        else
            GLRegistry.Instance.BindBuffer(BufferTarget.ElementArrayBuffer, null);

        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public void SetElementBuffer<T>(TypedArray<T> elementArray) where T : struct => SetElementBuffer(elementArray.Buffer);

    public void EnableAttribute(int location)
    {
        vao.EnableVertexAttribute(location);
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }
    public void DisableAttribute(int location)
    {
        vao.DisableVertexAttribute(location);
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public void DrawVertices(PrimitiveType primitiveType, int count, int offset = 0)
    {
        vao.Bind();
        GL.DrawArrays(primitiveType, offset, count);
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public void DrawElements(PrimitiveType primitiveType, int count, DrawElementsType elementType, int elementOffset = 0, int vertexOffset = 0)
    {
        vao.Bind();
        if (vertexOffset == 0)
            GL.DrawElements(primitiveType, count, elementType, elementOffset);
        else
            GL.DrawElementsBaseVertex(primitiveType, count, elementType, elementOffset, vertexOffset);
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    public void Clear()
    {
        vao.Dispose();
        vao = new();
        VertexArray.Unbind(); // Important to not implicitly capture the next bound EBO
    }

    IEnumerable<VertexAttributeInfo> GenerateVertexInfo<T>() where T : struct
    {
        var structLayout = typeof(T).StructLayoutAttribute;
        if (structLayout is null || structLayout.Value != LayoutKind.Sequential)
            throw new ArgumentException($"Type must have {nameof(StructLayoutAttribute)} with {nameof(LayoutKind)} set to {LayoutKind.Sequential}.");

        var vertexAttribFields = Util.ReflectionHelper.GetFieldsWithAttribute<T, VertexAttribAttribute>();
        foreach ((FieldInfo fieldInfo, VertexAttribAttribute attrib) in vertexAttribFields)
        {
            Util.GetVertexAttribType(fieldInfo.FieldType, out VertexAttribPointerType attribType, out int componentCount);
            if (attrib.Normalized && !Util.IsIntegerVertexAttribType(attribType))
                throw new ArgumentException("Normalized can only be set to true for integer vertex attribute types.");

            int offset = (int)Marshal.OffsetOf<T>(fieldInfo.Name);

            yield return new VertexAttributeInfo(
                attrib.Location,
                componentCount,
                offset,
                attribType,
                attrib.Normalized,
                Marshal.SizeOf<T>());
        }
    }
    void IDisposableExt.Free() => vao.Dispose();

    ~VertexSpecification() => this.Dispose();
}