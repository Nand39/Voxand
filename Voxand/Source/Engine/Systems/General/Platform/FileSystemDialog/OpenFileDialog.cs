using Voxand.Helpers.Interop.Win32.FileSystemDialog;
using static Voxand.Helpers.Interop.Win32.FileSystemDialog.FileSystemDialogAPI;

namespace Voxand.Engine.Systems.General.Platform.FileSystemDialog;
public class OpenFileDialog
{
    public string Title { get; set; } = "Open file";
    public string InitialDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string? Filters { get; set; }
    public nint WindowHandle { get; set; } = 0;
    public string? OpenDialog()
    {
        using FileDialogParameters result = OpenFile(
            title: Title,
            initialDirectory: InitialDirectory,
            windowHandle: WindowHandle,
            flags: OpenFileNameFlags.Explorer | OpenFileNameFlags.FileMustExist | OpenFileNameFlags.PathMustExist | OpenFileNameFlags.NoChangeDir, 
            filter: Filters,
            status: out bool status);

        return status ? result.FilePath : null;
    }
}