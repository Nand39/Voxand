using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Voxand.Engine.Systems.Services.General;
public interface IWindowService
{
    Window Window { get; }
    CursorModeValue CursorMode { get; set; }
    nint Win32Handle { get; }
}