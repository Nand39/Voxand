using OpenTK.Windowing.GraphicsLibraryFramework;
using Voxand.Engine.Systems.Services.General;

namespace Voxand.Engine.Systems.Common;
public class WindowService : IWindowService
{
    public WindowService(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        Window = window;
    }
    public Window Window { get; }
    public unsafe CursorModeValue CursorMode
    {
        get => cursorMode;
        set
        {
            cursorMode = value;
            GLFW.SetInputMode(Window.WindowPtr, CursorStateAttribute.Cursor, cursorMode);
        }
    }
    CursorModeValue cursorMode;
}