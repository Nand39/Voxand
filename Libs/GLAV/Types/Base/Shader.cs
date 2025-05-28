using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Helpers.Public.Exceptions;

namespace GLAV.Types;
public class Shader : GLResource
{
    public Shader(params ShaderPart[] shaderAttachments)
    {
        Handle.resourceType = GLResourceType.ShaderProgram;
        Handle.id = GL.CreateProgram();
        Label = "Unnamed shader";

        for (int i = 0; i < shaderAttachments.Length; i++)
            GL.AttachShader(Handle.id, shaderAttachments[i].Handle.id);

        GL.LinkProgram(Handle.id);

        if (GetParameter(GetProgramParameterName.LinkStatus) == (int)All.False)
        {
            string infoLog = GL.GetProgramInfoLog(Handle.id);
            throw new ShaderLinkingException(infoLog);
        }

        for (int i = 0; i < shaderAttachments.Length; i++)
            GL.DetachShader(Handle.id, shaderAttachments[i].Handle.id);

        for (int i = 0; i < shaderAttachments.Length; i++)
            shaderAttachments[i].Dispose();
    }

    public int GetParameter(GetProgramParameterName param)
    {
        GL.GetProgram(Handle.id, param, out int result);
        return result;
    }

    public void Use() => GLRegistry.Instance.UseProgram(Handle.id);

    #region Introspection
    public int GetInterfaceProperty(ProgramInterface targetInterface, ProgramInterfaceParameter targetParameter)
    {
        GL.GetProgramInterface(
            program: Handle.id,
            programInterface: targetInterface,
            pname: targetParameter,
            @params: out int result);
        return result;
    }

    public int GetResourceInfo(ProgramInterface targetInterface, int resourceIndex, ProgramProperty prop)
    {
        GL.GetProgramResource(
                program: Handle.id,
                programInterface: targetInterface,
                index: resourceIndex,
                propCount: 1,
                props: ref prop,
                bufSize: sizeof(ProgramProperty),
                length: out int l,
                @params: out int output);
        return output;
    }
    public int[] GetResourceInfo(ProgramInterface targetInterface, int resourceIndex, ProgramProperty[] props)
    {
        int[] output = new int[props.Length];
        GL.GetProgramResource(
                program: Handle.id,
                programInterface: targetInterface,
                index: resourceIndex,
                propCount: props.Length,
                props: props,
                bufSize: sizeof(ProgramProperty) * props.Length,
                length: out int l,
                @params: output);
        return output;
    }

    public string GetResourceName(ProgramInterface targetInterface, int resourceIndex)
    {
        ProgramProperty prop = ProgramProperty.NameLength;
        int length = GetResourceInfo(targetInterface, resourceIndex, prop);
        GL.GetProgramResourceName(Handle.id, targetInterface, resourceIndex, length, out int l, out string name);
        return name;
    }
    #endregion

    #region Communication
    

    #region Uniform setters per type
    public void SetUniform(int location, int value) { Use(); GL.Uniform1(location, value); }
    public void SetUniform(int location, uint value) { Use(); GL.Uniform1(location, value); }
    public void SetUniform(int location, float value) { Use(); GL.Uniform1(location, value); }
    public void SetUniform(int location, Vector2 value) { Use(); GL.Uniform2(location, value); }
    public void SetUniform(int location, Vector2i value) { Use(); GL.Uniform2(location, value); }
    public void SetUniform(int location, Vector3 value) { Use(); GL.Uniform3(location, value); }
    public void SetUniform(int location, Vector3i value) { Use(); GL.Uniform3(location, value); }
    public void SetUniform(int location, Vector4 value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Vector4i value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Color4 value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Quaternion value) { Use(); GL.Uniform4(location, value); }
    public void SetUniform(int location, Matrix2 value) { Use(); GL.UniformMatrix2(location, true, ref value); }
    public void SetUniform(int location, Matrix3 value) { Use(); GL.UniformMatrix3(location, true, ref value); }
    public void SetUniform(int location, Matrix4 value) { Use(); GL.UniformMatrix4(location, true, ref value); }
    #endregion

    #region Uniform getters per type
    public unsafe int GetUniformInt(int location)
    {
        Use();
        GL.GetUniform(Handle.id, location, out int value);
        return value;
    }

    public unsafe uint GetUniformUInt(int location)
    {
        Use();
        int value = 0;
        GL.GetUniform(Handle.id, location, &value);
        return *(uint*)&value;
    }

    public unsafe float GetUniformFloat(int location)
    {
        Use();
        GL.GetUniform(Handle.id, location, out float value);
        return value;
    }

    public unsafe Vector2 GetUniformVec2(int location)
    {
        Use();
        float* buffer = stackalloc float[2];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Vector2*)buffer;
    }

    public unsafe Vector2i GetUniformVec2i(int location)
    {
        Use();
        int* buffer = stackalloc int[2];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Vector2i*)buffer;
    }

    public unsafe Vector3 GetUniformVec3(int location)
    {
        Use();
        float* buffer = stackalloc float[3];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Vector3*)buffer;
    }

    public unsafe Vector3i GetUniformVec3i(int location)
    {
        Use();
        int* buffer = stackalloc int[3];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Vector3i*)buffer;
    }

    public unsafe Vector4 GetUniformVec4(int location)
    {
        Use();
        float* buffer = stackalloc float[4];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Vector4*)buffer;
    }

    public unsafe Vector4i GetUniformVec4i(int location)
    {
        Use();
        int* buffer = stackalloc int[4];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Vector4i*)buffer;
    }

    public unsafe Color4 GetUniformColor4(int location)
    {
        Use();
        float* buffer = stackalloc float[4];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Color4*)buffer;
    }

    public unsafe Quaternion GetUniformQuat(int location)
    {
        Use();
        float* buffer = stackalloc float[4];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Quaternion*)buffer;
    }

    public unsafe Matrix2 GetUniformMat2x2(int location)
    {
        Use();
        float* buffer = stackalloc float[4];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Matrix2*)buffer;
    }

    public unsafe Matrix3 GetUniformMat3x3(int location)
    {
        Use();
        float* buffer = stackalloc float[9];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Matrix3*)buffer;
    }

    public unsafe Matrix4 GetUniformMat4x4(int location)
    {
        Use();
        float* buffer = stackalloc float[16];
        GL.GetUniform(Handle.id, location, buffer);
        return *(Matrix4*)buffer;
    }

    #endregion

    public unsafe object GetUniformByGLType(int location, int type)
    {
        Use();
        switch ((All)type)
        {
            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported uniform type");

            case All.Float:
                {
                    GL.GetUniform(Handle.id, location, out float value);
                    return value;
                }

            case All.FloatVec2:
                {
                    float* buffer = stackalloc float[2];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector2*)buffer;
                }

            case All.FloatVec3:
                {
                    float* buffer = stackalloc float[3];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector3*)buffer;
                }

            case All.FloatVec4:
                {
                    float* buffer = stackalloc float[4];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector4*)buffer;
                }

            case All.Double:
                {
                    GL.GetUniform(Handle.id, location, out double value);
                    return value;
                }

            case All.DoubleVec2:
                {
                    double* buffer = stackalloc double[2];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector2d*)buffer;
                }

            case All.DoubleVec3:
                {
                    double* buffer = stackalloc double[3];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector3d*)buffer;
                }

            case All.DoubleVec4:
                {
                    double* buffer = stackalloc double[4];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector4d*)buffer;
                }

            case All.Int:
                {
                    GL.GetUniform(Handle.id, location, out int value);
                    return value;
                }

            case All.IntVec2:
                {
                    int* buffer = stackalloc int[2];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector2i*)buffer;
                }

            case All.IntVec3:
                {
                    int* buffer = stackalloc int[3];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector3i*)buffer;
                }

            case All.IntVec4:
                {
                    int* buffer = stackalloc int[4];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Vector4i*)buffer;
                }

            case All.UnsignedInt:
                {
                    int value = 0;
                    GL.GetUniform(Handle.id, location, &value);
                    return *(uint*)value;
                }

            case All.Bool:
                {
                    GL.GetUniform(Handle.id, location, out int value);
                    return value != 0;
                }

            case All.BoolVec2:
                throw new NotImplementedException();

            case All.BoolVec3:
                throw new NotImplementedException();

            case All.BoolVec4:
                throw new NotImplementedException();

            case All.FloatMat2:
                {
                    float* buffer = stackalloc float[4];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix2*)buffer;
                }

            case All.FloatMat3:
                {
                    float* buffer = stackalloc float[9];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix3*)buffer;
                }

            case All.FloatMat4:
                {
                    float* buffer = stackalloc float[16];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix4*)buffer;
                }

            case All.FloatMat2x3:
                {
                    float* buffer = stackalloc float[6];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix2x3*)buffer;
                }

            case All.FloatMat2x4:
                {
                    float* buffer = stackalloc float[8];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix2x4*)buffer;
                }

            case All.FloatMat3x2:
                {
                    float* buffer = stackalloc float[6];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix3x2*)buffer;
                }

            case All.FloatMat3x4:
                {
                    float* buffer = stackalloc float[12];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix3x4*)buffer;
                }

            case All.FloatMat4x2:
                {
                    float* buffer = stackalloc float[8];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix4x2*)buffer;
                }

            case All.FloatMat4x3:
                {
                    float* buffer = stackalloc float[12];
                    GL.GetUniform(Handle.id, location, buffer);
                    return *(Matrix4x3*)buffer;
                }

            case All.Sampler1D:
            case All.Sampler2D:
            case All.Sampler3D:
            case All.SamplerCube:
            case All.Sampler1DShadow:
            case All.Sampler2DShadow:
            case All.Sampler1DArray:
            case All.Sampler2DArray:
            case All.Sampler1DArrayShadow:
            case All.Sampler2DArrayShadow:
            case All.Sampler2DMultisample:
            case All.Sampler2DMultisampleArray:
            case All.SamplerCubeShadow:
            case All.SamplerBuffer:
            case All.Sampler2DRect:
            case All.Sampler2DRectShadow:
            case All.IntSampler1D:
            case All.IntSampler2D:
            case All.IntSampler3D:
            case All.IntSamplerCube:
            case All.IntSampler1DArray:
            case All.IntSampler2DArray:
            case All.IntSampler2DMultisample:
            case All.IntSampler2DMultisampleArray:
            case All.IntSamplerBuffer:
            case All.IntSampler2DRect:
            case All.UnsignedIntSampler1D:
            case All.UnsignedIntSampler2D:
            case All.UnsignedIntSampler3D:
            case All.UnsignedIntSamplerCube:
            case All.UnsignedIntSampler1DArray:
            case All.UnsignedIntSampler2DArray:
            case All.UnsignedIntSampler2DMultisample:
            case All.UnsignedIntSampler2DMultisampleArray:
            case All.UnsignedIntSamplerBuffer:
            case All.UnsignedIntSampler2DRect:
            case All.Image1D:
            case All.Image2D:
            case All.Image3D:
            case All.ImageCube:
            case All.Image1DArray:
            case All.Image2DArray:
            case All.Image2DMultisample:
            case All.Image2DMultisampleArray:
            case All.ImageBuffer:
            case All.Image2DRect:
            case All.IntImage1D:
            case All.IntImage2D:
            case All.IntImage3D:
            case All.IntImageCube:
            case All.IntImage1DArray:
            case All.IntImage2DArray:
            case All.IntImage2DMultisample:
            case All.IntImage2DMultisampleArray:
            case All.IntImageBuffer:
            case All.IntImage2DRect:
            case All.UnsignedIntImage1D:
            case All.UnsignedIntImage2D:
            case All.UnsignedIntImage3D:
            case All.UnsignedIntImageCube:
            case All.UnsignedIntImage1DArray:
            case All.UnsignedIntImage2DArray:
            case All.UnsignedIntImage2DMultisample:
            case All.UnsignedIntImage2DMultisampleArray:
            case All.UnsignedIntImageBuffer:
            case All.UnsignedIntImage2DRect:
                {
                    GL.GetUniform(Handle.id, location, out int value);
                    return value;
                }
        }
    }
    public void SetUniformByGLType(int location, int type, object value)
    {
        Use();
        switch ((All)type)
        {
            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported uniform type");

            case All.Float:
                GL.Uniform1(location, (float)value);
                break;

            case All.FloatVec2:
                GL.Uniform2(location, (Vector2)value);
                break;

            case All.FloatVec3:
                GL.Uniform3(location, (Vector3)value);
                break;

            case All.FloatVec4:
                GL.Uniform4(location, (Vector4)value);
                break;

            case All.Double:
                GL.Uniform1(location, (double)value);
                break;

            case All.DoubleVec2:
                {
                    Vector2d vect = (Vector2d)value;
                    GL.Uniform2(location, vect.X, vect.Y);
                    break;
                }
            case All.DoubleVec3:
                {
                    Vector3d vect = (Vector3d)value;
                    GL.Uniform3(location, vect.X, vect.Y, vect.Z);
                    break;
                }
            case All.DoubleVec4:
                {
                    Vector4d vect = (Vector4d)value;
                    GL.Uniform4(location, vect.X, vect.Y, vect.Z, vect.W);
                    break;
                }

            case All.Int:
                GL.Uniform1(location, (int)value);
                break;

            case All.IntVec2:
                GL.Uniform2(location, (Vector2i)value);
                break;

            case All.IntVec3:
                GL.Uniform3(location, (Vector3i)value);
                break;

            case All.IntVec4:
                GL.Uniform4(location, (Vector4i)value);
                break;

            case All.UnsignedInt:
                GL.Uniform1(location, (uint)value);
                break;

            case All.Bool:
                GL.Uniform1(location, (bool)value ? 1 : 0);
                break;

            case All.BoolVec2:
                throw new NotImplementedException();

            case All.BoolVec3:
                throw new NotImplementedException();

            case All.BoolVec4:
                throw new NotImplementedException();

            case All.FloatMat2:
                {
                    Matrix2 mat = (Matrix2)value;
                    GL.UniformMatrix2(location, false, ref mat);
                    break;
                }
            case All.FloatMat3:
                {
                    Matrix3 mat = (Matrix3)value;
                    GL.UniformMatrix3(location, false, ref mat);
                    break;
                }
            case All.FloatMat4:
                {
                    Matrix4 mat = (Matrix4)value;
                    GL.UniformMatrix4(location, false, ref mat);
                    break;
                }

            case All.FloatMat2x3:
                {
                    Matrix2x3 mat = (Matrix2x3)value;
                    GL.UniformMatrix2x3(location, false, ref mat);
                    break;
                }

            case All.FloatMat2x4:
                {
                    Matrix2x4 mat = (Matrix2x4)value;
                    GL.UniformMatrix2x4(location, false, ref mat);
                    break;
                }

            case All.FloatMat3x2:
                {
                    Matrix3x2 mat = (Matrix3x2)value;
                    GL.UniformMatrix3x2(location, false, ref mat);
                    break;
                }

            case All.FloatMat3x4:
                {
                    Matrix3x4 mat = (Matrix3x4)value;
                    GL.UniformMatrix3x4(location, false, ref mat);
                    break;
                }

            case All.FloatMat4x2:
                {
                    Matrix4x2 mat = (Matrix4x2)value;
                    GL.UniformMatrix4x2(location, false, ref mat);
                    break;
                }

            case All.FloatMat4x3:
                {
                    Matrix4x3 mat = (Matrix4x3)value;
                    GL.UniformMatrix4x3(location, false, ref mat);
                    break;
                }

            case All.Sampler1D:
            case All.Sampler2D:
            case All.Sampler3D:
            case All.SamplerCube:
            case All.Sampler1DShadow:
            case All.Sampler2DShadow:
            case All.Sampler1DArray:
            case All.Sampler2DArray:
            case All.Sampler1DArrayShadow:
            case All.Sampler2DArrayShadow:
            case All.Sampler2DMultisample:
            case All.Sampler2DMultisampleArray:
            case All.SamplerCubeShadow:
            case All.SamplerBuffer:
            case All.Sampler2DRect:
            case All.Sampler2DRectShadow:
            case All.IntSampler1D:
            case All.IntSampler2D:
            case All.IntSampler3D:
            case All.IntSamplerCube:
            case All.IntSampler1DArray:
            case All.IntSampler2DArray:
            case All.IntSampler2DMultisample:
            case All.IntSampler2DMultisampleArray:
            case All.IntSamplerBuffer:
            case All.IntSampler2DRect:
            case All.UnsignedIntSampler1D:
            case All.UnsignedIntSampler2D:
            case All.UnsignedIntSampler3D:
            case All.UnsignedIntSamplerCube:
            case All.UnsignedIntSampler1DArray:
            case All.UnsignedIntSampler2DArray:
            case All.UnsignedIntSampler2DMultisample:
            case All.UnsignedIntSampler2DMultisampleArray:
            case All.UnsignedIntSamplerBuffer:
            case All.UnsignedIntSampler2DRect:

            case All.Image1D:
            case All.Image2D:
            case All.Image3D:
            case All.ImageCube:
            case All.Image1DArray:
            case All.Image2DArray:
            case All.Image2DMultisample:
            case All.Image2DMultisampleArray:
            case All.ImageBuffer:
            case All.Image2DRect:
            case All.IntImage1D:
            case All.IntImage2D:
            case All.IntImage3D:
            case All.IntImageCube:
            case All.IntImage1DArray:
            case All.IntImage2DArray:
            case All.IntImage2DMultisample:
            case All.IntImage2DMultisampleArray:
            case All.IntImageBuffer:
            case All.IntImage2DRect:
            case All.UnsignedIntImage1D:
            case All.UnsignedIntImage2D:
            case All.UnsignedIntImage3D:
            case All.UnsignedIntImageCube:
            case All.UnsignedIntImage1DArray:
            case All.UnsignedIntImage2DArray:
            case All.UnsignedIntImage2DMultisample:
            case All.UnsignedIntImage2DMultisampleArray:
            case All.UnsignedIntImageBuffer:
            case All.UnsignedIntImage2DRect:
                GL.Uniform1(location, (int)value);
                break;
        }
    }

    public void SetShaderStorageBufferBinding(int index, int binding)
    {
        GL.ShaderStorageBlockBinding(Handle.id, index, binding);
    }
    public int GetShaderStorageBufferBinding(int index)
    {
        return GetResourceInfo(ProgramInterface.ShaderStorageBlock, index, ProgramProperty.BufferBinding);
    }

    #endregion

    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GLRegistry.Instance.DeleteProgram(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GLRegistry.Instance.DeleteProgram(Handle.id));
    }
}