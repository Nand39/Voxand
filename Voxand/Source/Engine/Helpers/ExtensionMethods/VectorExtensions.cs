using Nvec4 = System.Numerics.Vector4;
using Nvec3 = System.Numerics.Vector3;
using Nvec2 = System.Numerics.Vector2;

using OpenTK.Mathematics;

namespace Voxand.Helpers.ExtensionMethods;
public static class VectorTKExtensions
{
    #region TK-Num vector conversion 

    // Float-based vectors
    public static Nvec2 AsNum(this Vector2 vector)
    {
        return new Nvec2(vector.X, vector.Y);
    }
    public static Nvec3 AsNum(this Vector3 vector)
    {
        return new Nvec3(vector.X, vector.Y, vector.Z);
    }
    public static Nvec4 AsNum(this Vector4 vector)
    {
        return new Nvec4(vector.X, vector.Y, vector.Z, vector.W);
    }
    // Integer-based vectors
    public static Nvec2 AsNum(this Vector2i vector)
    {
        return new Nvec2(vector.X, vector.Y);
    }
    public static Nvec3 AsNum(this Vector3i vector)
    {
        return new Nvec3(vector.X, vector.Y, vector.Z);
    }
    public static Nvec4 AsNum(this Vector4i vector)
    {
        return new Nvec4(vector.X, vector.Y, vector.Z, vector.W);
    }
    #endregion

    #region Min

    // Integer vectors
    public static int Min(this Vector2i vect)
    {
        return vect.X < vect.Y ? vect.X : vect.Y;
    }
    public static int Min(this Vector3i vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? vect.X : vect.Z
                               : vect.Y < vect.Z ? vect.Y : vect.Z;
    }
    public static int Min(this Vector4i vect)
    {
        if (vect.X < vect.Y)
            if (vect.X < vect.Z)
                return vect.X < vect.W ? vect.X : vect.W;
        else
            if (vect.Y < vect.Z)
                return vect.Y < vect.W ? vect.Y : vect.W;
        return vect.Z < vect.W ? vect.Z : vect.W;
    }

    // Float vectors
    public static float Min(this Vector2 vect)
    {
        return vect.X < vect.Y ? vect.X : vect.Y;
    }
    public static float Min(this Vector3 vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? vect.X : vect.Z
                               : vect.Y < vect.Z ? vect.Y : vect.Z;
    }
    public static float Min(this Vector4 vect)
    {
        if (vect.X < vect.Y)
            if (vect.X < vect.Z)
                return vect.X < vect.W ? vect.X : vect.W;
        else
            if (vect.Y < vect.Z)
                return vect.Y < vect.W ? vect.Y : vect.W;
        return vect.Z < vect.W ? vect.Z : vect.W;
    }
    #endregion

    #region Max

    // Integer vectors
    public static int Max(this Vector2i vect)
    {
        return vect.X > vect.Y ? vect.X : vect.Y;
    }
    public static int Max(this Vector3i vect)
    {
        return vect.X > vect.Y ? vect.X > vect.Z ? vect.X : vect.Z
                               : vect.Y > vect.Z ? vect.Y : vect.Z;
    }
    public static int Max(this Vector4i vect)
    {
        if (vect.X > vect.Y)
            if (vect.X > vect.Z)
                return vect.X > vect.W ? vect.X : vect.W;
        else
            if (vect.Y > vect.Z)
                return vect.Y > vect.W ? vect.Y : vect.W;
        return vect.Z > vect.W ? vect.Z : vect.W;
    }

    // Float vectors
    public static float Max(this Vector2 vect)
    {
        return vect.X > vect.Y ? vect.X : vect.Y;
    }
    public static float Max(this Vector3 vect)
    {
        return vect.X > vect.Y ? vect.X > vect.Z ? vect.X : vect.Z
                               : vect.Y > vect.Z ? vect.Y : vect.Z;
    }
    public static float Max(this Vector4 vect)
    {
        if (vect.X > vect.Y)
            if (vect.X > vect.Z)
                return vect.X > vect.W ? vect.X : vect.W;
            else
            if (vect.Y > vect.Z)
                return vect.Y > vect.W ? vect.Y : vect.W;
        return vect.Z > vect.W ? vect.Z : vect.W;
    }
    #endregion

    #region IndexOfMin

    // Integer vectors
    public static int IndexOfMin(this Vector2i vect)
    {
        return vect.X < vect.Y ? 0 : 1;
    }
    public static int IndexOfMin(this Vector3i vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? 0 : 2
                               : vect.Y < vect.Z ? 1 : 2;
    }
    public static int IndexOfMin(this Vector4i vect)
    {
        if (vect.X < vect.Y)
            if (vect.X < vect.Z)
                return vect.X < vect.W ? 0 : 3;
        else
            if (vect.Y < vect.Z)
                return vect.Y < vect.W ? 1 : 3;
        return vect.Z < vect.W ? 2 : 3;
    }

    // Float vectors
    public static int IndexOfMin(this Vector2 vect)
    {
        return vect.X < vect.Y ? 0 : 1;
    }
    public static int IndexOfMin(this Vector3 vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? 0 : 2
                               : vect.Y < vect.Z ? 1 : 2;
    }
    public static int IndexOfMin(this Vector4 vect)
    {
        if (vect.X < vect.Y)
            if (vect.X < vect.Z)
                return vect.X < vect.W ? 0 : 3;
        else
            if (vect.Y < vect.Z)
                return vect.Y < vect.W ? 1 : 3;
        return vect.Z < vect.W ? 2 : 3;
    }
    #endregion

    #region IndexOfMax

    // Integer-based vectors
    public static int IndexOfMax(this Vector2i vect)
    {
        return vect.X > vect.Y ? 0 : 1;
    }
    public static int IndexOfMax(this Vector3i vect)
    {
        return vect.X > vect.Y ? vect.X > vect.Z ? 0 : 2
                               : vect.Y > vect.Z ? 1 : 2;
    }
    public static int IndexOfMax(this Vector4i vect)
    {
        if (vect.X > vect.Y)
            if (vect.X > vect.Z)
                return vect.X > vect.W ? 0 : 3;
            else
            if (vect.Y > vect.Z)
                return vect.Y > vect.W ? 1 : 3;
        return vect.Z > vect.W ? 2 : 3;
    }

    // Float-based vectors
    public static int IndexOfMax(this Vector2 vect)
    {
        return vect.X > vect.Y ? 0 : 1;
    }
    public static int IndexOfMax(this Vector3 vect)
    {
        return vect.X > vect.Y ? vect.X > vect.Z ? 0 : 2
                               : vect.Y > vect.Z ? 1 : 2;
    }
    public static int IndexOfMax(this Vector4 vect)
    {
        if (vect.X > vect.Y)
            if (vect.X > vect.Z)
                return vect.X > vect.W ? 0 : 3;
            else
            if (vect.Y > vect.Z)
                return vect.Y > vect.W ? 1 : 3;
        return vect.Z > vect.W ? 2 : 3;
    }
    #endregion

    #region Abs

    #region Mutable

    // Integer-based vectors
    public static void Abs(this Vector2i vect)
    {
        vect.X = Math.Abs(vect.X);
        vect.Y = Math.Abs(vect.Y);
    }
    public static void Abs(this Vector3i vect)
    {
        vect.X = Math.Abs(vect.X);
        vect.Y = Math.Abs(vect.Y);
        vect.Z = Math.Abs(vect.Z);
    }
    public static void Abs(this Vector4i vect)
    {
        vect.X = Math.Abs(vect.X);
        vect.Y = Math.Abs(vect.Y);
        vect.Z = Math.Abs(vect.Z);
        vect.W = Math.Abs(vect.W);
    }

    // Float-based vectors
    public static void Abs(this Vector2 vect)
    {
        vect.X = Math.Abs(vect.X);
        vect.Y = Math.Abs(vect.Y);
    }
    public static void Abs(this Vector3 vect)
    {
        vect.X = Math.Abs(vect.X);
        vect.Y = Math.Abs(vect.Y);
        vect.Z = Math.Abs(vect.Z);
    }
    public static void Abs(this Vector4 vect)
    {
        vect.X = Math.Abs(vect.X);
        vect.Y = Math.Abs(vect.Y);
        vect.Z = Math.Abs(vect.Z);
        vect.W = Math.Abs(vect.W);
    }
    #endregion

    #region Immutable

    // Integer-based vectors
    public static Vector2i AsAbs(this Vector2i vect) => new Vector2i(Math.Abs(vect.X), Math.Abs(vect.Y));
    public static Vector3i AsAbs(this Vector3i vect) => new Vector3i(Math.Abs(vect.X), Math.Abs(vect.Y), Math.Abs(vect.Z));
    public static Vector4i AsAbs(this Vector4i vect) => new Vector4i(Math.Abs(vect.X), Math.Abs(vect.Y), Math.Abs(vect.Z), Math.Abs(vect.W));

    // Float-based vectors
    public static Vector2 AsAbs(this Vector2 vect) => new Vector2(Math.Abs(vect.X), Math.Abs(vect.Y));
    public static Vector3 AsAbs(this Vector3 vect) => new Vector3(Math.Abs(vect.X), Math.Abs(vect.Y), Math.Abs(vect.Z));
    public static Vector4 AsAbs(this Vector4 vect) => new Vector4(Math.Abs(vect.X), Math.Abs(vect.Y), Math.Abs(vect.Z), Math.Abs(vect.W));
    #endregion

    #endregion

    #region Sum

    // Integer-based vectors
    public static int Sum(this Vector2i vect) => vect.X + vect.Y;
    public static int Sum(this Vector3i vect) => vect.X + vect.Y + vect.Z;
    public static int Sum(this Vector4i vect) => vect.X + vect.Y + vect.Z + vect.W;

    // Float-based vectors
    public static float Sum(this Vector2 vect) => vect.X + vect.Y;
    public static float Sum(this Vector3 vect) => vect.X + vect.Y + vect.Z;
    public static float Sum(this Vector4 vect) => vect.X + vect.Y + vect.Z + vect.W;
    #endregion

    #region AsIndex
    public static int AsIndexYZX(this Vector3i vect, Vector3i bounds) => vect.Y * bounds.Z * bounds.X + vect.Z * bounds.X + vect.X;
    public static int AsIndexYX(this Vector2i vect, Vector2i bounds) => vect.Y * bounds.X + vect.X;
    #endregion

    #region SafeNormalize
    public static void SafeNormalize(this ref Vector2 vect)
    {
        if (vect.X == 0 && vect.Y == 0)
        {
            vect.X = 0;
            vect.Y = 0;
        }
        vect.Normalize();
    }
    public static void SafeNormalize(this ref Vector3 vect)
    {
        if (vect.X == 0 && vect.Y == 0 && vect.Z == 0)
        {
            vect.X = 0;
            vect.Y = 0;
            vect.Z = 0;
        }
        vect.Normalize();
    }
    public static void SafeNormalize(this ref Vector4 vect)
    {
        if (vect.X == 0 && vect.Y == 0 && vect.Z == 0 && vect.W == 0)
        {
            vect.X = 0;
            vect.Y = 0;
            vect.Z = 0;
            vect.W = 0;
        }
        vect.Normalize();
    }
    public static Vector2 SafeNormalized(this Vector2 vect)
    {
        if (vect.X == 0 && vect.Y == 0)
            return Vector2.Zero;
        return vect.Normalized();
    }
    public static Vector3 SafeNormalized(this Vector3 vect)
    {
        if (vect.X == 0 && vect.Y == 0 && vect.Z == 0)
            return Vector3.Zero;
        return vect.Normalized();
    }
    public static Vector4 SafeNormalized(this Vector4 vect)
    {
        if (vect.X == 0 && vect.Y == 0 && vect.Z == 0 && vect.W == 0)
            return Vector4.Zero;
        return vect.Normalized();
    }
    #endregion

    #region Inbounds

    // Integer-based vectors
    public static bool Inbounds(this Vector2i vect, Vector2i min, Vector2i max)
    {
        return vect.X >= min.X && vect.X < max.X &&
               vect.Y >= min.Y && vect.Y < max.Y;
    }
    public static bool Inbounds(this Vector3i vect, Vector3i min, Vector3i max)
    {
        return vect.X >= min.X && vect.X < max.X &&
               vect.Y >= min.Y && vect.Y < max.Y &&
               vect.Z >= min.Z && vect.Z < max.Z;
    }

    // Float-based vectors
    public static bool Inbounds(this Vector2 vect, Vector2 min, Vector2 max)
    {
        return vect.X >= min.X && vect.X < max.X &&
               vect.Y >= min.Y && vect.Y < max.Y;
    }
    public static bool Inbounds(this Vector2 vect, Vector2i min, Vector2i max)
    {
        return vect.X >= min.X && vect.X < max.X &&
               vect.Y >= min.Y && vect.Y < max.Y;
    }
    public static bool Inbounds(this Vector3 vect, Vector3 min, Vector3 max)
    {
        return vect.X >= min.X && vect.X < max.X &&
               vect.Y >= min.Y && vect.Y < max.Y &&
               vect.Z >= min.Z && vect.Z < max.Z;
    }
    public static bool Inbounds(this Vector3 vect, Vector3i min, Vector3i max)
    {
        return vect.X >= min.X && vect.X < max.X &&
               vect.Y >= min.Y && vect.Y < max.Y &&
               vect.Z >= min.Z && vect.Z < max.Z;
    }
    #endregion

    #region Axis ratio
    public static float Ratio(this Vector2i vect) => (float)vect.X / vect.Y;
    public static float Ratio(this Vector2 vect) => vect.X / vect.Y;
    #endregion

    #region Bitwise operations

    #region Bitshift

    #region To the right
    public static Vector2i BitshiftRight(this Vector2i vect, int n) => new(vect.X >> n, vect.Y >> n);
    public static Vector3i BitshiftRight(this Vector3i vect, int n) => new(vect.X >> n, vect.Y >> n, vect.Z >> n);
    public static Vector4i BitshiftRight(this Vector4i vect, int n) => new(vect.X >> n, vect.Y >> n, vect.Z >> n, vect.W >> n);
    #endregion

    #region To the left
    public static Vector2i BitshiftLeft(this Vector2i vect, int n) => new(vect.X << n, vect.Y << n);
    public static Vector3i BitshiftLeft(this Vector3i vect, int n) => new(vect.X << n, vect.Y << n, vect.Z << n);
    public static Vector4i BitshiftLeft(this Vector4i vect, int n) => new(vect.X << n, vect.Y << n, vect.Z << n, vect.W << n);
    #endregion

    #endregion

    #region And
    public static Vector2i BitwiseAnd(this Vector2i vect, int n) => new(vect.X & n, vect.Y & n);
    public static Vector3i BitwiseAnd(this Vector3i vect, int n) => new(vect.X & n, vect.Y & n, vect.Z & n);
    public static Vector4i BitwiseAnd(this Vector4i vect, int n) => new(vect.X & n, vect.Y & n, vect.Z & n, vect.W & n);
    public static Vector2i BitwiseAnd(this Vector2i vect, Vector2i other) => new(vect.X & other.X, vect.Y & other.Y);
    public static Vector3i BitwiseAnd(this Vector3i vect, Vector3i other) => new(vect.X & other.X, vect.Y & other.Y, vect.Z & other.Z);
    public static Vector4i BitwiseAnd(this Vector4i vect, Vector4i other) => new(vect.X & other.X, vect.Y & other.Y, vect.Z & other.Z, vect.W & other.W);
    #endregion

    #endregion

    #region Per component arithmetic
    
    #region Add

    // Integer-based vectors
    public static Vector2i Add(this Vector2i vect, int n) => new(vect.X + n, vect.Y + n);
    public static Vector3i Add(this Vector3i vect, int n) => new(vect.X + n, vect.Y + n, vect.Z + n);
    public static Vector4i Add(this Vector4i vect, int n) => new(vect.X + n, vect.Y + n, vect.Z + n, vect.W + n);

    // Float-based vectors
    public static Vector2 Add(this Vector2 vect, float n) => new(vect.X + n, vect.Y + n);
    public static Vector3 Add(this Vector3 vect, float n) => new(vect.X + n, vect.Y + n, vect.Z + n);
    public static Vector4 Add(this Vector4 vect, float n) => new(vect.X + n, vect.Y + n, vect.Z + n, vect.W + n);
    #endregion

    #region Sub

    // Integer-based vectors
    public static Vector2i Sub(this Vector2i vect, int n) => new(vect.X - n, vect.Y - n);
    public static Vector3i Sub(this Vector3i vect, int n) => new(vect.X - n, vect.Y - n, vect.Z - n);
    public static Vector4i Sub(this Vector4i vect, int n) => new(vect.X - n, vect.Y - n, vect.Z - n, vect.W - n);

    // Float-based vectors
    public static Vector2 Sub(this Vector2 vect, float n) => new(vect.X - n, vect.Y - n);
    public static Vector3 Sub(this Vector3 vect, float n) => new(vect.X - n, vect.Y - n, vect.Z - n);
    public static Vector4 Sub(this Vector4 vect, float n) => new(vect.X - n, vect.Y - n, vect.Z - n, vect.W - n);
    #endregion

    #region Pow
    public static Vector2 Pow(this Vector2 vect, float n) => new(MathF.Pow(vect.X, n), MathF.Pow(vect.Y, n));
    public static Vector3 Pow(this Vector3 vect, float n) => new(MathF.Pow(vect.X, n), MathF.Pow(vect.Y, n), MathF.Pow(vect.Z, n));
    public static Vector4 Pow(this Vector4 vect, float n) => new(MathF.Pow(vect.X, n), MathF.Pow(vect.Y, n), MathF.Pow(vect.Z, n), MathF.Pow(vect.W, n));
    #endregion

    #endregion

    #region Transformation
    #region Rotation
    public static void Rotate(this Vector2 vect, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        vect.X = vect.X * cos - vect.Y * sin;
        vect.Y = vect.X * sin + vect.Y * cos;
    }
    public static Vector2 Rotated(this Vector2 vect, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(vect.X * cos - vect.Y * sin, vect.X * sin + vect.Y * cos);
    }
    #endregion
    #endregion
}
public static class VecotorNumExtensions
{
    #region Num-TK vector conversion 
    public static Vector2 AsTK(this Nvec2 vector) => new(vector.X, vector.Y);
    public static Vector3 AsTK(this Nvec3 vector) => new(vector.X, vector.Y, vector.Z);
    public static Vector4 AsTK(this Nvec4 vector) => new(vector.X, vector.Y, vector.Z, vector.W);

    #endregion
}