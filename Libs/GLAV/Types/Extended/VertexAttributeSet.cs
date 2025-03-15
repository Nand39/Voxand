using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Data;
using GLAV.Helpers.Internal;
using DisposableExt;

namespace GLAV.Types.Extended;
public class VertexAttributeSet : IDisposableExt
{
    VertexArray vao;

    public DisposeHelper DisposeHelper => throw new NotImplementedException();

    public VertexAttributeSet()
    {
        vao = new();
    }

    public void Use() => vao.Bind();

    public unsafe void AddAttributes<VertexType>(Buffer source) where VertexType : struct
    {
        List<VertexAttributeInfo> vertexAttribInfos = GenerateVertexInfo<VertexType>();
        
        vao.Bind();
        foreach (VertexAttributeInfo attribInfo in vertexAttribInfos)
            vao.SetVertexAttributePointer(attribInfo, source);
    }

    public void EnableAttribute(int location) => vao.EnableVertexAttribute(location);
    public void DisableAttribute(int location) => vao.DisableVertexAttribute(location);

    public void Clear()
    {
        vao.Dispose();
        vao = new();
    }

    unsafe List<VertexAttributeInfo> GenerateVertexInfo<T>() where T : struct
    {
        var structLayout = typeof(T).StructLayoutAttribute;
        if (structLayout is null || structLayout.Value != LayoutKind.Sequential)
            throw new ArgumentException($"Type must have {nameof(StructLayoutAttribute)} with {nameof(LayoutKind)} set to {LayoutKind.Sequential}.");

        List<VertexAttributeInfo> attribInfos = new();
        Util.ReflectionHelper.ForEachFieldWithAttribute<T, VertexDataAttribute>((field, attrib) =>
        {
            Util.GetVertexAttribType(field.FieldType, out VertexAttribPointerType attribType, out int componentCount);
            if (attrib.Normalized && !Util.IsIntegerVertexAttribType(attribType))
                throw new ArgumentException("Normalized can only be set to true for integer vertex attribute types.");

            int offset = (int)Marshal.OffsetOf<T>(field.Name);
            attribInfos.Add(new(attrib.Location, componentCount, offset, attribType, attrib.Normalized, sizeof(T)));
        });
        return attribInfos;
    }

    public void Free()
    {
        vao.Dispose();
    }
}