using System.Runtime.InteropServices;

namespace GLAV.Data;

[StructLayout(LayoutKind.Sequential)]
public struct Vec4i8
{
    public byte X, Y, Z, W;
    public Vec4i8(byte x, byte y, byte z, byte w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
    public unsafe Vec4i8(uint packed)
    {
        X = (byte)(packed & 0xFF);
        Y = (byte)((packed >> 8) & 0xFF);
        Z = (byte)((packed >> 16) & 0xFF);
        W = (byte)((packed >> 24) & 0xFF);
    }
}