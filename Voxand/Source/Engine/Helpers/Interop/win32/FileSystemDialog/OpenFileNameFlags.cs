namespace Voxand.Helpers.Interop.Win32.FileSystemDialog;

[Flags]
public enum OpenFileNameFlags : int
{
    None = 0,

    /// <summary>
    /// Creates a read-only file.
    /// </summary>
    ReadOnly = 1 << 0,

    /// <summary>
    /// Displays a prompt if the selected file already exists.
    /// </summary>
    OverwritePrompt = 1 << 1,

    /// <summary>
    /// Hides the read-only checkbox.
    /// </summary>
    HideReadOnly = 1 << 2,

    /// <summary>
    /// Restores the current directory to its original value if the user changes the directory while searching for files.
    /// </summary>
    NoChangeDir = 1 << 3,

    /// <summary>
    /// Displays the Help button in the dialog box.
    /// </summary>
    ShowHelp = 1 << 4,

    /// <summary>
    /// Enables the hook function specified in the OPENFILENAME structure.
    /// </summary>
    EnableHook = 1 << 5,

    /// <summary>
    /// Enables the template specified by the lpTemplateName member of the OPENFILENAME structure.
    /// </summary>
    EnableTemplate = 1 << 6,

    /// <summary>
    /// Indicates that the hInstance member of the OPENFILENAME structure is a handle to a data block that contains a preloaded dialog box template.
    /// </summary>
    EnableTemplateHandle = 1 << 7,

    /// <summary>
    /// Does not validate file names.
    /// </summary>
    NoValidate = 1 << 8,

    /// <summary>
    /// Allows multiple files to be selected.
    /// </summary>
    AllowMultiSelect = 1 << 9,

    /// <summary>
    /// Returns OFN_EXTENSIONDIFFERENT if the user types a file name that has a different extension from the default extension.
    /// </summary>
    ExtensionDifferent = 1 << 10,

    /// <summary>
    /// The user can enter only paths that exist. If the user enters an invalid path, the dialog box displays a warning message.
    /// </summary>
    PathMustExist = 1 << 11,

    /// <summary>
    /// The user can enter only names of existing files in the File Name entry field. If the user enters an invalid file name, the dialog box displays a warning message.
    /// </summary>
    FileMustExist = 1 << 12,

    /// <summary>
    /// Displays a message box if the user specifies a file that does not exist.
    /// </summary>
    CreatePrompt = 1 << 13,

    /// <summary>
    /// The returned file name has a path and file name that are valid for sharing.
    /// </summary>
    ShareAware = 1 << 14,

    /// <summary>
    /// Does not return read-only files.
    /// </summary>
    NoReadOnlyReturn = 1 << 15,

    /// <summary>
    /// Does not create a temporary file.
    /// </summary>
    NoTestFileCreate = 1 << 16,

    /// <summary>
    /// Hides the Network button.
    /// </summary>
    NoNetworkButton = 1 << 17,

    /// <summary>
    /// For systems that support long file names, this flag causes the dialog box to display file names in the 8.3 format.
    /// </summary>
    NoLongNames = 1 << 18,

    /// <summary>
    /// Uses the Explorer-style open and save dialog boxes.
    /// </summary>
    Explorer = 1 << 19,

    /// <summary>
    /// Does not dereference shell links (shortcuts).
    /// </summary>
    NoDereferenceLinks = 1 << 20,

    /// <summary>
    /// Forces the dialog box to display the full paths and file names for files that have long file names.
    /// </summary>
    LongNames = 1 << 21,

    /// <summary>
    /// Enables the hook function specified by the lpfnHook member of the OPENFILENAME structure for WM_NOTIFY messages.
    /// </summary>
    EnableIncludeNotify = 1 << 22,

    /// <summary>
    /// Enables the sizing grip on the common dialog box.
    /// </summary>
    EnableSizing = 1 << 23,

    /// <summary>
    /// Prevents the system from adding the selected file to the Most Recently Used (MRU) list.
    /// </summary>
    DontAddToRecent = 1 << 25,

    /// <summary>
    /// Forces the showing of hidden and system files, which are not shown by default.
    /// </summary>
    ForceShowHidden = 1 << 28
}