using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Voxand;

Window window = new (GameWindowSettings.Default, new NativeWindowSettings()
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
});

window.Run();