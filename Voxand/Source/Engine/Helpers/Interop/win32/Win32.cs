using System.Runtime.InteropServices;

namespace Voxand.Helpers.Interop.Win32;
public static class Win32
{
    [DllImport("user32.dll")]
    public static extern int MessageBox(nint hWnd, string text, string caption, uint type);
}