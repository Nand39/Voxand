using DisposableExt;
using System.Runtime.InteropServices;
using System.Text;

namespace Voxand.Helpers.Interop.Win32.FileSystemDialog;

internal unsafe class FileDialogParameters : IDisposableExt
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

    public void* WindowOwner
    {
        get => openFileName.hwndOwner;
        set => openFileName.hwndOwner = value;
    }

    public void* Instance
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

    public void* CustomData
    {
        get => openFileName.lCustData;
        set => openFileName.lCustData = value;
    }

    public void* HookFunction
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
            if (openFileName.lpstrFile is null)
                return string.Empty;

            return Marshal.PtrToStringUni((nint)openFileName.lpstrFile) ?? string.Empty;
        }
        set
        {
            if (value is not null)
            {
                byte[] fileBytes = Encoding.Unicode.GetBytes(value);
                int length = Math.Min(fileBytes.Length, (MaxPathLength - 1) * sizeof(char));
                Marshal.Copy(fileBytes, 0, (nint)openFileName.lpstrFile, length);
                Util.MemClear((void*)(((nint)openFileName.lpstrFile) + length), fileBytes.Length * sizeof(byte));
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
            if (openFileName.lpstrFileTitle is null)
                return string.Empty;

            return Marshal.PtrToStringUni((nint)openFileName.lpstrFileTitle) ?? string.Empty;
        }
    }

    void IDisposableExt.Free()
    {
        if (openFileName.lpstrFile is not null)
        {
            Marshal.FreeHGlobal((nint)openFileName.lpstrFile);
            openFileName.lpstrFile = null;
        }

        if (openFileName.lpstrFileTitle is not null)
        {
            Marshal.FreeHGlobal((nint)openFileName.lpstrFileTitle);
            openFileName.lpstrFileTitle = null;
        }
    }

    ~FileDialogParameters() => this.Dispose();
}