using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Voxand.Engine.Systems.Services.General;
interface IWindowService
{
    Window Window { get; }
    CursorModeValue CursorMode { get; set; }
}