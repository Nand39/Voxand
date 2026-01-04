using System.Runtime.InteropServices;

namespace Voxand.Helpers.Interop.Win32.FileSystemDialog;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal unsafe struct OpenFileName
{
    public int lStructSize;
    public void* hwndOwner;
    public void* hInstance;
    public string lpstrFilter;
    public string lpstrCustomFilter;
    public int nMaxCustFilter;
    public int nFilterIndex;
    public void* lpstrFile;
    public int nMaxFile;
    public void* lpstrFileTitle;
    public int nMaxFileTitle;
    public string lpstrInitialDir;
    public string lpstrTitle;
    public OpenFileNameFlags Flags;
    public short nFileOffset;
    public short nFileExtension;
    public string lpstrDefExt;
    public void* lCustData;
    public void* lpfnHook;
    public string lpTemplateName;
    public void* pvReserved;
    public int dwReserved;
    public int FlagsEx;
}