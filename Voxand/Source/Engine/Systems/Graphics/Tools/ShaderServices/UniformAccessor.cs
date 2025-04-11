using GLAV.Types;
using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
public struct UniformAccessor<T> where T : struct
{
    int location;
    Shader shader;

    public UniformAccessor(int location, Shader target)
    {
        shader = target;
        this.location = location;
    }

    public unsafe T Get()
    {
        Type type = typeof(T);

        if (type == typeof(int))
        {
            int value = shader.GetUniformInt(location);
            return *(T*)&value;
        }
        else if (type == typeof(uint))
        {
            uint value = shader.GetUniformUInt(location);
            return *(T*)&value;
        }
        else if (type == typeof(float))
        {
            float value = shader.GetUniformFloat(location);
            return *(T*)&value;
        }
        else if (type == typeof(Vector2))
        {
            Vector2 value = shader.GetUniformVec2(location);
            return *(T*)&value;
        }
        else if (type == typeof(Vector2i))
        {
            Vector2i value = shader.GetUniformVec2i(location);
            return *(T*)&value;
        }
        else if (type == typeof(Vector3))
        {
            Vector3 value = shader.GetUniformVec3(location);
            return *(T*)&value;
        }
        else if (type == typeof(Vector3i))
        {
            Vector3i value = shader.GetUniformVec3i(location);
            return *(T*)&value;
        }
        else if (type == typeof(Vector4))
        {
            Vector4 value = shader.GetUniformVec4(location);
            return *(T*)&value;
        }
        else if (type == typeof(Vector4i))
        {
            Vector4i value = shader.GetUniformVec4i(location);
            return *(T*)&value;
        }
        else if (type == typeof(Color4))
        {
            Color4 value = shader.GetUniformColor4(location);
            return *(T*)&value;
        }
        else if (type == typeof(Quaternion))
        {
            Quaternion value = shader.GetUniformQuat(location);
            return *(T*)&value;
        }
        else if (type == typeof(Matrix2))
        {
            Matrix2 value = shader.GetUniformMat2x2(location);
            return *(T*)&value;
        }
        else if (type == typeof(Matrix3))
        {
            Matrix3 value = shader.GetUniformMat3x3(location);
            return *(T*)&value;
        }
        else if (type == typeof(Matrix4))
        {
            Matrix4 value = shader.GetUniformMat4x4(location);
            return *(T*)&value;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported uniform type: {type}");
        }
    }

    public unsafe void Set(T value)
    {
        Type type = typeof(T);

        if (type == typeof(int))
        {
            shader.SetUniform(location, *(int*)&value);
        }
        else if (type == typeof(uint))
        {
            shader.SetUniform(location, *(uint*)&value);
        }
        else if (type == typeof(float))
        {
            shader.SetUniform(location, *(float*)&value);
        }
        else if (type == typeof(Vector2))
        {
            shader.SetUniform(location, *(Vector2*)&value);
        }
        else if (type == typeof(Vector2i))
        {
            shader.SetUniform(location, *(Vector2i*)&value);
        }
        else if (type == typeof(Vector3))
        {
            shader.SetUniform(location, *(Vector3*)&value);
        }
        else if (type == typeof(Vector3i))
        {
            shader.SetUniform(location, *(Vector3i*)&value);
        }
        else if (type == typeof(Vector4))
        {
            shader.SetUniform(location, *(Vector4*)&value);
        }
        else if (type == typeof(Vector4i))
        {
            shader.SetUniform(location, *(Vector4i*)&value);
        }
        else if (type == typeof(Color4))
        {
            shader.SetUniform(location, *(Color4*)&value);
        }
        else if (type == typeof(Quaternion))
        {
            shader.SetUniform(location, *(Quaternion*)&value);
        }
        else if (type == typeof(Matrix2))
        {
            shader.SetUniform(location, *(Matrix2*)&value);
        }
        else if (type == typeof(Matrix3))
        {
            shader.SetUniform(location, *(Matrix3*)&value);
        }
        else if (type == typeof(Matrix4))
        {
            shader.SetUniform(location, *(Matrix4*)&value);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported uniform type: {type}");
        }
    }
}