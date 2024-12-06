using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

Voxand.Window game;
game = new(GameWindowSettings.Default, new NativeWindowSettings()
{
    Title = "Voxand Renderer",
    ClientSize = new OpenTK.Mathematics.Vector2i(780, 780),
    WindowBorder = WindowBorder.Resizable,
    StartVisible = false,
    StartFocused = true,
    API = ContextAPI.OpenGL,
    APIVersion = new Version(4, 3),
    Profile = ContextProfile.Core
})
{
    VSync = VSyncMode.On
};

game.Run();