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
    }

    public void Use() => vao.Bind();

    public unsafe void AddAttributeSource<VertexType>(Buffer source) where VertexType : struct
    {
        vao.Bind();
        foreach (VertexAttributeInfo attribInfo in GenerateVertexInfo<VertexType>())
            vao.SetVertexAttributePointer(attribInfo, source);
    }

    public unsafe void AddAttributeSource<VertexType>(TypedArray<VertexType> source) where VertexType : struct
    {
        vao.Bind();
        foreach (VertexAttributeInfo attribInfo in GenerateVertexInfo<VertexType>())
            vao.SetVertexAttributePointer(attribInfo, source.Buffer);
    }

    public void EnableAttribute(int location) => vao.EnableVertexAttribute(location);
    public void DisableAttribute(int location) => vao.DisableVertexAttribute(location);

    public void Clear()
    {
        vao.Dispose();
        vao = new();
    }

    IEnumerable<VertexAttributeInfo> GenerateVertexInfo<T>() where T : struct
    {
        var structLayout = typeof(T).StructLayoutAttribute;
        if (structLayout is null || structLayout.Value != LayoutKind.Sequential)
            throw new ArgumentException($"Type must have {nameof(StructLayoutAttribute)} with {nameof(LayoutKind)} set to {LayoutKind.Sequential}.");

        var vertexAttribFields = Util.ReflectionHelper.GetFieldsWithAttribute<T, VertexAttribAttribute>();
        foreach ((FieldInfo fieldInfo, VertexAttribAttribute attrib) attribField in vertexAttribFields)
        {
            Util.GetVertexAttribType(attribField.fieldInfo.FieldType, out VertexAttribPointerType attribType, out int componentCount);
            if (attribField.attrib.Normalized && !Util.IsIntegerVertexAttribType(attribType))
                throw new ArgumentException("Normalized can only be set to true for integer vertex attribute types.");

            int offset = (int)Marshal.OffsetOf<T>(attribField.fieldInfo.Name);

            yield return new VertexAttributeInfo(
                attribField.attrib.Location,
                componentCount,
                offset,
                attribType,
                attribField.attrib.Normalized,
                Marshal.SizeOf<T>());
        }
    }
    public void Free() => vao.Dispose();
}