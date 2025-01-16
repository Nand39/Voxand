#pragma warning disable CS8618 //Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using GLAV;

using Voxand.Content;
using Voxand.Engine.ExecutionControl;
using Voxand.Helpers;
using Voxand.UI;
using Voxand.Helpers.Interop;
using Voxand.Helpers.Exceptions;

namespace Voxand;
public sealed class Window : GameWindow
{
    static Window windowInstance;
    public static Window Instance => windowInstance;

    public ExecutionManager ExecutionManager { get; private set; }
    public ContentManager Content { get; private set; }
    public ImGuiController ImGuiController { get; private set; }

    public bool IsMinimized { get; private set; }
    Window(GameWindowSettings windowSettings, NativeWindowSettings nativeWindowSettings)
        : base(windowSettings, nativeWindowSettings)
    {
    }

    public static void Initialize(GameWindowSettings windowSettings, NativeWindowSettings nativeWindowSettings)
    {
        windowInstance = new Window(windowSettings, nativeWindowSettings);
    }

    protected override void OnLoad()
    {
        AppDomain.CurrentDomain.UnhandledException += OnException;
        GLRegistry.Initialize(Context);
        
        CenterWindow();
        IsVisible = true;
        Util.ClientSize = ClientSize;

        base.OnLoad();

        string? AsmName = Assembly.GetExecutingAssembly().GetName().Name;

        Content = new ContentManager("Content");

        ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);

        ExecutionManager = new MainExecutionManager(this);
        ExecutionManager.Load();

        MouseWheel += (args) =>
        {
            ImGuiController.MouseScroll(args.Offset);
        };
    }

    protected override void OnUnload()
    {
        ExecutionManager.Unload();
        base.OnUnload();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        ExecutionManager.Update(args);
        ImGuiController.Update(this, (float)args.Time);
        base.OnUpdateFrame(args);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GLRegistry.Instance.ProcessOpenGLActions();
        ExecutionManager.Render(args);
        ImGuiController.Render();
        Context.SwapBuffers();
        base.OnRenderFrame(args);
    }

    public T TryAccessExecutionManager<T>() where T : class
    {
        return ExecutionManager as T ??
            throw new FeatureUnsupportedException($"Current execution manager does not support {nameof(T)} feature.");
    }
    void OnException(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine("oops :/");
        Exception ex = (args.ExceptionObject as Exception)!;
        NativeFuncs.Win.MessageBox(0, $"Voxand has crashed lmao\n{ex.Message}\nStack trace:\n{ex.StackTrace ?? "!bad luck, no stack trace"}", "oops", 0x00000000u);
        Close();
        Environment.Exit(ex.HResult);
    }
    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        Util.ClientSize = ClientSize;
        ImGuiController.WindowResized(ClientSize.X, ClientSize.Y);
        GL.Viewport(0, 0, args.Width, args.Height);
        ExecutionManager.OnResize(args);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        ImGuiController.PressChar((uint)e.Unicode);
        base.OnTextInput(e);
    }

    protected override void OnMinimized(MinimizedEventArgs e)
    {
        IsMinimized = e.IsMinimized;
        base.OnMinimized(e);
    }
}
