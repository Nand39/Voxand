using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Sparkvox.src.ExecutionControl;
using Voxand;

NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
{
    Title = "Sparkvox: Voxand Demo",
    ClientSize = new OpenTK.Mathematics.Vector2i(780, 780),
    WindowBorder = WindowBorder.Resizable,
    StartVisible = false,
    StartFocused = true,
    API = ContextAPI.OpenGL,
    APIVersion = new Version(4, 3),
    Profile = ContextProfile.Core,
    Flags = ContextFlags.Debug,
    Vsync = VSyncMode.On,
};
GameWindowSettings windowSettings = GameWindowSettings.Default;

PrimaryManager executionManager = new PrimaryManager();

Window window = new(windowSettings, nativeWindowSettings, executionManager);

window.Run();

//E:\projects\programming\VoxelEngine\Example\bin\Release\net8.0\resources\UI\ImGuiStyles\style.json