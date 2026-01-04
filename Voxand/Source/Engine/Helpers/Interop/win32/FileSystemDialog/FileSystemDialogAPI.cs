using System.Runtime.InteropServices;

namespace Voxand.Helpers.Interop.Win32.FileSystemDialog;

public unsafe static class FileSystemDialogAPI
{

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] ref OpenFileName ofn);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] ref OpenFileName ofn);

    const string defaultFilter = "All Files (*.*)\0*.*\0\0";

    internal static FileDialogParameters OpenFile(string title, string initialDirectory, void* windowHandle, OpenFileNameFlags flags, string? filter, out bool status)
    {
        FileDialogParameters dialogParams = new();
        dialogParams.WindowOwner = windowHandle;
        dialogParams.InitialDirectory = initialDirectory;
        dialogParams.Title = title;
        dialogParams.Filter = filter ?? defaultFilter;
        dialogParams.Flags = flags;

        status = GetOpenFileName(ref dialogParams.InnerStruct);
        return dialogParams;
    }

    internal static FileDialogParameters SaveFile(string title, string initialDirectory, void* windowHandle, OpenFileNameFlags flags, string? defaultFileName, string? defaultExtension, string? filter, out bool status)
    {
        FileDialogParameters dialogParams = new();

        dialogParams.FilePath = defaultFileName;
        dialogParams.WindowOwner = windowHandle;
        dialogParams.DefaultExtension = defaultExtension;
        dialogParams.InitialDirectory = initialDirectory;
        dialogParams.Title = title;
        dialogParams.Filter = filter ?? defaultFilter;
        dialogParams.Flags = flags;

        status = GetSaveFileName(ref dialogParams.InnerStruct);
        return dialogParams;
    }
}