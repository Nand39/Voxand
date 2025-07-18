using Voxand.Helpers.Interop.Win32.FileSystemDialog;

namespace Voxand.Engine.Systems.General.Platform.FileSystemDialog;
public class SaveFileDialog
{
    public string Title { get; set; } = "Save file";
    public string InitialDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string? DefaultFileName { get; set; }
    public string? DefaultExtension { get; set; }
    public string? Filters { get; set; }
    public nint WindowHandle { get; set; } = 0;
    public string? OpenDialog()
    {
        using FileDialogParameters result = FileSystemDialogAPI.SaveFile(Title, InitialDirectory, WindowHandle, OpenFileNameFlags.Explorer | OpenFileNameFlags.PathMustExist | OpenFileNameFlags.NoChangeDir | OpenFileNameFlags.OverwritePrompt, DefaultFileName, DefaultExtension, Filters, out bool status);
        return status ? result.FilePath : null;
    }
}