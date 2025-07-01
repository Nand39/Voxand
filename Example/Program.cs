using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Voxand;
using Voxand.App;

NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
{
    Title = "Voxand Renderer",
    ClientSize = new OpenTK.Mathematics.Vector2i(780, 780),
    WindowBorder = WindowBorder.Resizable,
    StartVisible = false,
    StartFocused = true,
    API = ContextAPI.OpenGL,
    APIVersion = new Version(4, 3),
    Profile = ContextProfile.Core,
    Flags = ContextFlags.Default,
    Vsync = VSyncMode.On,
};
GameWindowSettings windowSettings = GameWindowSettings.Default;

MainExecutionManager executionManager = new MainExecutionManager();

Window window = new(windowSettings, nativeWindowSettings, executionManager);

window.Run();