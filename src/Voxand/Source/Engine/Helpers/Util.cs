using System.Runtime.CompilerServices;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using Voxand.Engine.Systems.Voxels;
using Voxand.Engine.Graphics;
using Voxand.Helpers.Utility;

namespace Voxand.Helpers;

public static class Util
{
    public const float BYTE_NORMALIZE = 1f / 255f;
    public const float SQRT_TWO = 1.414213f;
    public const float DEG2RAD = 180 / MathF.PI;
    
    public static readonly Random Random = new();
    public static readonly int ProcessorCount = Environment.ProcessorCount;
    public static VoxelMap CurrentMap;
    public static Vector2i ClientSize;
    public static FrameTimeData FrameTimeData = new();
    public static Vector3 RotateY(Vector3 vect, float r)
    {
        float sin = MathF.Sin(r);
        float cos = MathF.Cos(r);
        return new Vector3(vect.X * cos - vect.Z * sin, vect.Y, vect.X * sin + vect.Z * cos);
    }
    public static bool Inbounds(ref readonly Vector3 p, ref readonly Vector3 max)
    {
        return p.X >= 0 && p.X < max.X &&
               p.Y >= 0 && p.Y < max.Y &&
               p.Z >= 0 && p.Z < max.Z;
    }
    public static bool Inbounds(ref readonly Vector3 p, ref readonly Vector3i max)
    {
        return p.X >= 0 && p.X < max.X &&
               p.Y >= 0 && p.Y < max.Y &&
               p.Z >= 0 && p.Z < max.Z;
    }
    public static bool Inbounds(ref readonly Vector3i p, ref readonly Vector3i max)
    {
        return p.X >= 0 && p.X < max.X &&
               p.Y >= 0 && p.Y < max.Y &&
               p.Z >= 0 && p.Z < max.Z;
    }

    public static Vector3i ClampVector(Vector3i min, Vector3i max, Vector3i vector)
    {
        return new(
            vector.X < min.X ? min.X : vector.X > max.X ? max.X : vector.X,
            vector.Y < min.Y ? min.Y : vector.Y > max.Y ? max.Y : vector.Y,
            vector.Z < min.Z ? min.Z : vector.Z > max.Z ? max.Z : vector.Z);
    }

    public static bool Inbounds(ref readonly Vector3i p, ref readonly Vector3i min, ref readonly Vector3i max)
    {
        return p.X >= min.X && p.X < max.X &&
               p.Y >= min.Y && p.Y < max.Y &&
               p.Z >= min.Z && p.Z < max.Z;
    }

    public static void CalcTaskingInfo(int tasksDispatched, int totalWorkSize, out int taskSize, out int taskRemains, out int taskCount)
    {
        taskSize = totalWorkSize / ProcessorCount;
        taskRemains = totalWorkSize - ProcessorCount * taskSize;
        taskCount = ProcessorCount;
    }

    public static double BytesConverter(long bytes, int order)
    {
        int shifting = order * 10;
        long megabytes = bytes >> shifting;
        double fraction = (bytes & ((1L << shifting) - 1)) / Math.Pow(1024, order);
        return megabytes + fraction;
    }

    public static Vector3 RotateVerticalByNormalIndex(Vector3 vect, int normalIndex)
    {
        switch (normalIndex)
        {
            default: return default;
            case 0: return new(-vect.Y, vect.X, vect.Z);
            case 1: return new(vect.Y, -vect.X, vect.Z);
            case 2: return new(-vect.X, -vect.Y, vect.Z);
            case 3: return vect;
            case 4: return new(vect.X, vect.Z, -vect.Y);
            case 5: return new(vect.X, -vect.Z, vect.Y);
        }
    }

    #region Math Helper

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Deg2Rad(Vector3 n)
    {
        return n * DEG2RAD;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Min(params int[] values)
    {
        int min = int.MaxValue;
        foreach(int value in values)
            min = min < value ? min : value;
        return min;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float VectorMin(Vector3 vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? vect.X : vect.Z 
                               : vect.Y < vect.Z ? vect.Y : vect.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VectorMin(Vector3i vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? vect.X : vect.Z
                               : vect.Y < vect.Z ? vect.Y : vect.Z;
    }

    #region Step Func
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Step(float edge, float x)
    {
        return x < edge ? 0 : 1;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Step(Vector3 edge, Vector3 vect)
    {
        return new(Step(edge.X, vect.X), Step(edge.Y, vect.Y), Step(edge.Z, vect.Z));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Step(Vector3i edge, Vector3i vect)
    {
        return new(Step(edge.X, vect.X), Step(edge.Y, vect.Y), Step(edge.Z, vect.Z));
    }
    #endregion

    #endregion

    #region Colors
    public static Vector3 Hex2Vec(string hexadecimalValue)
    {
        return new Vector3(
            int.Parse(hexadecimalValue.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hexadecimalValue.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hexadecimalValue.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)) * BYTE_NORMALIZE;
    }
    public static Vector3 GetMaterialSolidColor(VoxelMaterial material)
    {
        return material.color * 0.8f + material.emission * 0.2f;
    }
    #endregion

    #region OpenGL
    public static ShaderType? IdentifyShaderSource(string path)
    {
        string extension = Path.GetExtension(path);
        switch (extension)
        {
            default: return null;
            case ".vert": return ShaderType.VertexShader;
            case ".frag": return ShaderType.FragmentShader;
            case ".comp": return ShaderType.ComputeShader;
        }
    }

    #endregion
}
