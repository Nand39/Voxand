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

    // Float-based vectors
    public static float Min(this Vector2 vect)
    {
        return vect.X < vect.Y ? vect.X : vect.Y;
    }
    public static float Min(this Vector3 vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? vect.X : vect.Z
                               : vect.Y < vect.Z ? vect.Y : vect.Z;
    }

    // Integer-based vectors
    public static int Min(this Vector2i vect)
    {
        return vect.X < vect.Y ? vect.X : vect.Y;
    }
    public static int Min(this Vector3i vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? vect.X : vect.Z
                               : vect.Y < vect.Z ? vect.Y : vect.Z;
    }

    #endregion

    #region Max

    // Float-based vectors
    public static float Max(this Vector2 vect)
    {
        return vect.X > vect.Y ? vect.X : vect.Y;
    }
    public static float Max(this Vector3 vect)
    {
        return vect.X > vect.Y ? vect.X > vect.Z ? vect.X : vect.Z
                               : vect.Y > vect.Z ? vect.Y : vect.Z;
    }

    // Integer-based vectors
    public static int Max(this Vector2i vect)
    {
        return vect.X > vect.Y ? vect.X : vect.Y;
    }
    public static int Max(this Vector3i vect)
    {
        return vect.X > vect.Y ? vect.X > vect.Z ? vect.X : vect.Z
                               : vect.Y > vect.Z ? vect.Y : vect.Z;
    }

    #endregion

    // Lacks implementation for certain types
    #region IndexMin
    public static int IndexOfMin(this Vector3i vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? 0 : 2
                               : vect.Y < vect.Z ? 1 : 2;
    }
    public static int IndexOfMin(this Vector3 vect)
    {
        return vect.X < vect.Y ? vect.X < vect.Z ? 0 : 2
                               : vect.Y < vect.Z ? 1 : 2;
    }
    #endregion

    // Lacks implementation for certain types
    #region Abs
    public static void Abs(this Vector3i vect)
    {
        vect.X = vect.X < 0 ? -vect.X : vect.X;
        vect.Y = vect.Y < 0 ? -vect.Y : vect.Y;
        vect.Z = vect.Z < 0 ? -vect.Z : vect.Z;
    }
    public static Vector3i AsAbs(this Vector3i vect)
    {
        return new Vector3i(
            vect.X < 0 ? -vect.X : vect.X,
            vect.Y < 0 ? -vect.Y : vect.Y,
            vect.Z < 0 ? -vect.Z : vect.Z);
    }
    #endregion

    // Lacks implementation for certain types
    #region Sum
    public static int Sum(this Vector3i vect) => vect.X + vect.Y + vect.Z;
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
}
public static class VecotorNumExtensions
{
    #region Num-TK vector conversion 
    public static Vector2 AsTK(this Nvec2 vector) => new(vector.X, vector.Y);
    public static Vector3 AsTK(this Nvec3 vector) => new(vector.X, vector.Y, vector.Z);
    public static Vector4 AsTK(this Nvec4 vector) => new(vector.X, vector.Y, vector.Z, vector.W);

    #endregion
}