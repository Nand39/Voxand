using System.Runtime.InteropServices;
using System.Text;

using DisposableExt;

namespace Voxand.Helpers.Interop.Win32.FileSystemDialog;
internal class FileDialogParameters : IDisposableExt
{
    OpenFileName openFileName;

    public const int MaxPathLength = 4096;
    public const int MaxFileTitleLength = 256;

    public DisposeState DisposeState { get; }

    public FileDialogParameters()
    {
        openFileName = new OpenFileName();

        // Allocate memory for file path
        openFileName.lpstrFile = Util.AllocHInitialized(MaxPathLength * sizeof(char));
        openFileName.nMaxFile = MaxPathLength;

        // Allocate memory for file titles
        openFileName.lpstrFileTitle = Util.AllocHInitialized(MaxFileTitleLength * sizeof(char));
        openFileName.nMaxFileTitle = MaxFileTitleLength;

        openFileName.lStructSize = Marshal.SizeOf(typeof(OpenFileName));

        DisposeState = new(this);
    }

    public ref OpenFileName InnerStruct => ref openFileName;

    public nint WindowOwner
    {
        get => openFileName.hwndOwner;
        set => openFileName.hwndOwner = value;
    }

    public nint Instance
    {
        get => openFileName.hInstance;
        set => openFileName.hInstance = value;
    }

    public string Filter
    {
        get => openFileName.lpstrFilter;
        set => openFileName.lpstrFilter = value;
    }

    public string CustomFilter
    {
        get => openFileName.lpstrCustomFilter;
        set => openFileName.lpstrCustomFilter = value;
    }

    public int MaxCustomFilter
    {
        get => openFileName.nMaxCustFilter;
        set => openFileName.nMaxCustFilter = value;
    }

    public int FilterIndex
    {
        get => openFileName.nFilterIndex;
        set => openFileName.nFilterIndex = value;
    }

    public string InitialDirectory
    {
        get => openFileName.lpstrInitialDir;
        set => openFileName.lpstrInitialDir = value;
    }

    public string Title
    {
        get => openFileName.lpstrTitle;
        set => openFileName.lpstrTitle = value;
    }

    public OpenFileNameFlags Flags
    {
        get => openFileName.Flags;
        set => openFileName.Flags = value;
    }

    public short FileOffset
    {
        get => openFileName.nFileOffset;
        set => openFileName.nFileOffset = value;
    }

    public short FileExtension
    {
        get => openFileName.nFileExtension;
        set => openFileName.nFileExtension = value;
    }

    public string? DefaultExtension
    {
        get => openFileName.lpstrDefExt;
        set => openFileName.lpstrDefExt = value;
    }

    public nint CustomData
    {
        get => openFileName.lCustData;
        set => openFileName.lCustData = value;
    }

    public nint HookFunction
    {
        get => openFileName.lpfnHook;
        set => openFileName.lpfnHook = value;
    }

    public string TemplateName
    {
        get => openFileName.lpTemplateName;
        set => openFileName.lpTemplateName = value;
    }

    public int FlagsExtended
    {
        get => openFileName.FlagsEx;
        set => openFileName.FlagsEx = value;
    }

    public string? FilePath
    {
        get
        {
            if (openFileName.lpstrFile == nint.Zero)
                return string.Empty;

            return Marshal.PtrToStringUni(openFileName.lpstrFile) ?? string.Empty;
        }
        set
        {
            if (value is not null)
            {
                byte[] fileBytes = Encoding.Unicode.GetBytes(value);
                int length = Math.Min(fileBytes.Length, (MaxPathLength - 1) * sizeof(char));
                Marshal.Copy(fileBytes, 0, openFileName.lpstrFile, length);
                Util.MemClear(openFileName.lpstrFile + length, fileBytes.Length * sizeof(byte));
            }
            else
            {
                Util.MemClear(openFileName.lpstrFile, MaxPathLength);
            }
        }
    }

    public string FileTitle
    {
        get
        {
            if (openFileName.lpstrFileTitle == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringUni(openFileName.lpstrFileTitle) ?? string.Empty;
        }
    }

    void IDisposableExt.Free()
    {
        if (openFileName.lpstrFile != nint.Zero)
        {
            Marshal.FreeHGlobal(openFileName.lpstrFile);
            openFileName.lpstrFile = nint.Zero;
        }

        if (openFileName.lpstrFileTitle != nint.Zero)
        {
            Marshal.FreeHGlobal(openFileName.lpstrFileTitle);
            openFileName.lpstrFileTitle = nint.Zero;
        }
    }

    ~FileDialogParameters() => this.Dispose();
}