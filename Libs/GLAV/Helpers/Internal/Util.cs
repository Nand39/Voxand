using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using System.Diagnostics;
using System.Reflection;
using OpenTK.Mathematics;
using System;
using GLAV.Data;

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
        if (GL.GetError() != ErrorCode.NoError)
            Debugger.Break();
    }
    public static ObjectLabelIdentifier GetLabelIdentifier(GLResourceType type)
    {
        switch (type)
        {
            default: return ObjectLabelIdentifier.Buffer;
            case GLResourceType.Buffer: return ObjectLabelIdentifier.Buffer;
            case GLResourceType.Texture: return ObjectLabelIdentifier.Texture;
            case GLResourceType.VertexArray: return ObjectLabelIdentifier.VertexArray;
            case GLResourceType.ShaderPart: return ObjectLabelIdentifier.Shader;
            case GLResourceType.ShaderProgram: return ObjectLabelIdentifier.Program;
            case GLResourceType.Framebuffer: return ObjectLabelIdentifier.Framebuffer;
        }
    }

    public static BufferTargetFlags TargetToFlag(BufferTarget target) => target switch
    {
        BufferTarget.ParameterBuffer => BufferTargetFlags.ParameterBuffer,
        BufferTarget.ArrayBuffer => BufferTargetFlags.ArrayBuffer,
        BufferTarget.ElementArrayBuffer => BufferTargetFlags.ElementArrayBuffer,
        BufferTarget.PixelPackBuffer => BufferTargetFlags.PixelPackBuffer,
        BufferTarget.PixelUnpackBuffer => BufferTargetFlags.PixelUnpackBuffer,
        BufferTarget.UniformBuffer => BufferTargetFlags.UniformBuffer,
        BufferTarget.TextureBuffer => BufferTargetFlags.TextureBuffer,
        BufferTarget.TransformFeedbackBuffer => BufferTargetFlags.TransformFeedbackBuffer,
        BufferTarget.CopyReadBuffer => BufferTargetFlags.CopyReadBuffer,
        BufferTarget.CopyWriteBuffer => BufferTargetFlags.CopyWriteBuffer,
        BufferTarget.DrawIndirectBuffer => BufferTargetFlags.DrawIndirectBuffer,
        BufferTarget.ShaderStorageBuffer => BufferTargetFlags.ShaderStorageBuffer,
        BufferTarget.DispatchIndirectBuffer => BufferTargetFlags.DispatchIndirectBuffer,
        BufferTarget.QueryBuffer => BufferTargetFlags.QueryBuffer,
        BufferTarget.AtomicCounterBuffer => BufferTargetFlags.AtomicCounterBuffer,
        _ => throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported buffer target: {target}")
    };

    // AI generated (ChatGPT)
    public static TexLoadFormat GetSuitableTexLoadFormat(PixelInternalFormat storageFormat)
    {
        return storageFormat switch
        {
            // Float formats
            PixelInternalFormat.Rgba32f => new TexLoadFormat(PixelFormat.Rgba, PixelType.Float),
            PixelInternalFormat.Rgb32f => new TexLoadFormat(PixelFormat.Rgb, PixelType.Float),
            PixelInternalFormat.Rgba16f => new TexLoadFormat(PixelFormat.Rgba, PixelType.HalfFloat),
            PixelInternalFormat.Rgb16f => new TexLoadFormat(PixelFormat.Rgb, PixelType.HalfFloat),

            // Unsigned normalized formats
            PixelInternalFormat.Rgba8 => new TexLoadFormat(PixelFormat.Rgba, PixelType.UnsignedByte),
            PixelInternalFormat.Rgb8 => new TexLoadFormat(PixelFormat.Rgb, PixelType.UnsignedByte),

            // Integer formats
            PixelInternalFormat.Rgba8i => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.Byte),
            PixelInternalFormat.Rgba8ui => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.UnsignedByte),
            PixelInternalFormat.Rgb8i => new TexLoadFormat(PixelFormat.RgbInteger, PixelType.Byte),
            PixelInternalFormat.Rgb8ui => new TexLoadFormat(PixelFormat.RgbInteger, PixelType.UnsignedByte),
            PixelInternalFormat.Rgba32i => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.Int),
            PixelInternalFormat.Rgba32ui => new TexLoadFormat(PixelFormat.RgbaInteger, PixelType.UnsignedInt),

            // Depth formats
            PixelInternalFormat.DepthComponent24 => new TexLoadFormat(PixelFormat.DepthComponent, PixelType.UnsignedInt),
            PixelInternalFormat.DepthComponent32f => new TexLoadFormat(PixelFormat.DepthComponent, PixelType.Float),

            // Depth-stencil formats
            PixelInternalFormat.Depth24Stencil8 => new TexLoadFormat(PixelFormat.DepthStencil, PixelType.UnsignedInt248),
            PixelInternalFormat.Depth32fStencil8 => new TexLoadFormat(PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev),

            _ => throw new ArgumentException($"Unsupported storage format: {storageFormat}")
        };
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
        { typeof(bool), (VertexAttribPointerType.Byte, 1) },
        { typeof(Vec4i8), (VertexAttribPointerType.UnsignedByte, 4) }
    };

    public static bool IsIntegerVertexAttribType(VertexAttribPointerType type) => type switch
    {
        VertexAttribPointerType.Int => true,
        VertexAttribPointerType.Short => true,
        VertexAttribPointerType.Byte => true,
        VertexAttribPointerType.UnsignedInt => true,
        VertexAttribPointerType.UnsignedShort => true,
        VertexAttribPointerType.UnsignedByte => true,
        _ => false,
    };

    public static void GetVertexAttribType(Type fieldType, out VertexAttribPointerType attribType, out int componentCount)
    {
        if (!DataTypesToVertexAttribTypeInfos.TryGetValue(fieldType, out (VertexAttribPointerType attribType, int componentCount) attribTypeInfo))
            throw new ArgumentException($"Type {fieldType} is not supported as a vertex attribute.");

        attribType = attribTypeInfo.attribType;
        componentCount = attribTypeInfo.componentCount;
    }

    internal static class ReflectionHelper
    {
        public static IEnumerable<(FieldInfo field, AttribType attrib)> GetFieldsWithAttribute<T, AttribType>() 
            where AttribType : Attribute
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
                if (field.GetCustomAttribute<AttribType>() is AttribType attrib)
                    yield return (field, attrib);
        }

        public static IEnumerable<(FieldInfo, IEnumerable<AttribType>)> GetFieldsWithAttributes<T, AttribType>() 
            where AttribType : Attribute
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                var attribs = field.GetCustomAttributes<AttribType>();
                if (attribs.Any())
                    yield return (field, attribs);
            }
        }
    }
}