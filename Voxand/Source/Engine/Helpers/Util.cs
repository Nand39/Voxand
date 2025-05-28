using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.CompilerServices;
using Voxand.Engine.Systems.Voxels;
using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;
using Voxand.Helpers.UtilityObjects;

namespace Voxand.Helpers;

public static class Util
{
    public const float BYTE_NORMALIZE = 1f / 255f;
    public const float SQRT_TWO = 1.414213f;
    public const float DEG2RAD = MathF.PI / 180;
    
    public static readonly Random Random = new();
    public static readonly int ProcessorCount = Environment.ProcessorCount;
    public static Vector2i ClientSize;
    public static FrameTimeData FrameTimeData = new();
    public static Vector3 RotateY(Vector3 vect, float r)
    {
        float sin = MathF.Sin(r);
        float cos = MathF.Cos(r);
        return new Vector3(vect.X * cos - vect.Z * sin, vect.Y, vect.X * sin + vect.Z * cos);
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

    public static Vector3i VectorFromNormalIndex(int normalIndex)
    {
        switch (normalIndex)
        {
            default: return default;
            case 0: return new(-1, 0, 0);
            case 1: return new(1, 0, 0);
            case 2: return new(0, -1, 0);
            case 3: return new(0, 1, 0);
            case 4: return new(0, 0, -1);
            case 5: return new(0, 0, 1);
        }
    }

    public static Vector3i NormalIncrement(Vector3i vect, int normalIndex)
    {
        vect[normalIndex >> 2] += (normalIndex & 1) > 0 ? 1 : -1;
        return vect;
    }

    public static Vector3 RotateUnitYByNormalIndex(Vector3 vect, int normalIndex)
    {
        switch (normalIndex)
        {
            default: return default;
            case 0: return new(-vect.Y, vect.X, vect.Z);
            case 1: return new(vect.Y, -vect.X, vect.Z);
            case 2: return -vect;
            case 3: return vect;
            case 4: return new(vect.X, vect.Z, -vect.Y);
            case 5: return new(vect.X, -vect.Z, vect.Y);
        }
    }

    public static void OrderBounds(Vector3i vect0, Vector3i vect1, out Vector3i min, out Vector3i max)
    {
        min = new(Math.Min(vect0.X, vect1.X), Math.Min(vect0.Y, vect1.Y), Math.Min(vect0.Z, vect1.Z));
        max = new(Math.Max(vect0.X, vect1.X), Math.Max(vect0.Y, vect1.Y), Math.Max(vect0.Z, vect1.Z));
    }

    public static void LoopYZX(Vector3i min, Vector3i max, Action<Vector3i> action)
    {
        Vector3i pos = default;
        for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
            for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                for (pos.X = min.X; pos.X <= max.X; pos.X++)
                    action(pos);
    }

    public static int Mod(int a, int b) => (a %= b) < 0 ? a + b : a;

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

    #region Colors
    public static Vector3 Hex2Vec(string hexadecimalValue)
    {
        if (hexadecimalValue[0] != '#' || hexadecimalValue.Length != 7)
            throw new ArgumentException("Hexadecimal value must be in the format #RRGGBB");

        return new Vector3(
            int.Parse(hexadecimalValue.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hexadecimalValue.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hexadecimalValue.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber)) * BYTE_NORMALIZE;
    }
    public static Vector3 GetMaterialDisplayColor(VoxelMaterial material)
    {
        return material.baseColor * 0.8f + material.emissionColor * material.emissionIntensity * 0.2f;
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
