using System.Runtime.InteropServices;

namespace Voxand.Helpers.Interop;
public static class NativeFuncs
{
    public static class Win
    {
        [DllImport("user32.dll")]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}