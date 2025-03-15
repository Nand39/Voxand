using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using System.Diagnostics;
using System.Reflection;
using OpenTK.Mathematics;
using System;

namespace GLAV.Helpers.Internal;
internal static class Util
{
    public static void LabelResource(GLResourceHandle handle, string label)
    {
        ObjectLabelIdentifier objLabelId = GetLabelIdentifier(handle.resourceType);
        int maxLength = GL.GetInteger(GetPName.MaxLabelLength);

        if (label.Length > maxLength)
            throw new ArgumentException($"Lable string should not exceed character limit (currently {maxLength})");

        GL.ObjectLabel(objLabelId, handle.id, label.Length, label);
    }
    public static ObjectLabelIdentifier GetLabelIdentifier(GLResourceType type)
    {
        switch (type)
        {
            default: return ObjectLabelIdentifier.Buffer;
            case GLResourceType.Buffer: return ObjectLabelIdentifier.Buffer;
            case GLResourceType.Texture2D: return ObjectLabelIdentifier.Texture;
            case GLResourceType.VertexArray: return ObjectLabelIdentifier.VertexArray;
            case GLResourceType.ShaderPart: return ObjectLabelIdentifier.Shader;
            case GLResourceType.ShaderProgram: return ObjectLabelIdentifier.Program;
            case GLResourceType.Framebuffer: return ObjectLabelIdentifier.Framebuffer;
        }
    }

    static Dictionary<Type, (VertexAttribPointerType, int)> DataTypesToVertexAttribTypeInfos = new()
    {
        { typeof(float), (VertexAttribPointerType.Float, 1) },
        { typeof(int), (VertexAttribPointerType.Int, 1) },
        { typeof(uint), (VertexAttribPointerType.UnsignedInt, 1) },
        { typeof(byte), (VertexAttribPointerType.UnsignedByte, 1) },
        { typeof(short), (VertexAttribPointerType.Short, 1) },
        { typeof(ushort), (VertexAttribPointerType.UnsignedShort, 1) },
        { typeof(Vector2), (VertexAttribPointerType.Float, 2) },
        { typeof(Vector3), (VertexAttribPointerType.Float, 3) },
        { typeof(Vector4), (VertexAttribPointerType.Float, 4) },
        { typeof(double), (VertexAttribPointerType.Double, 1) },
        { typeof(sbyte), (VertexAttribPointerType.Byte, 1) },
        { typeof(bool), (VertexAttribPointerType.Byte, 1) } // Bool is represented with byte here
    };

    public static bool IsIntegerVertexAttribType(VertexAttribPointerType type)
    {
        return type switch
        {
            VertexAttribPointerType.Int => true,
            VertexAttribPointerType.Short => true,
            VertexAttribPointerType.Byte => true,
            VertexAttribPointerType.UnsignedInt => true,
            VertexAttribPointerType.UnsignedShort => true,
            VertexAttribPointerType.UnsignedByte => true,
            _ => false,
        };
    }

    public static void GetVertexAttribType(Type fieldType, out VertexAttribPointerType attribType, out int componentCount)
    {
        if (!DataTypesToVertexAttribTypeInfos.TryGetValue(fieldType, out (VertexAttribPointerType attribType, int componentCount) attribTypeInfo))
            throw new ArgumentException($"Type {fieldType} is not supported as a vertex attribute.");

        attribType = attribTypeInfo.attribType;
        componentCount = attribTypeInfo.componentCount;
    }

    internal static class ReflectionHelper
    {
        public static void ForEachField<T>(Action<FieldInfo> action)
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                action(fields[i]);
        }

        public static void ForEachFieldWithAttribute<T, AttribType>(Action<FieldInfo, AttribType> action) where AttribType : Attribute
        {
            ForEachField<T>((fieldInfo) =>
            {
                AttribType? attrib = fieldInfo.GetCustomAttribute<AttribType>();
                if (attrib is not null)
                    action(fieldInfo, attrib);
            });
        }

        public static void ForEachFieldWithAttributes<T, AttribType>(Action<FieldInfo, IEnumerable<AttribType>> action) where AttribType : Attribute
        {
            ForEachField<T>((fieldInfo) =>
            {
                var attribs = fieldInfo.GetCustomAttributes<AttribType>();
                if (attribs.Any())
                    action(fieldInfo, attribs);
            });
        }
    }
}