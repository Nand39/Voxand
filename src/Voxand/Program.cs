using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Voxand;
using Voxand.Helpers.Interop;

Voxand.Voxand game;
game = new(GameWindowSettings.Default, new NativeWindowSettings()
{
    Title = "Voxand",
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