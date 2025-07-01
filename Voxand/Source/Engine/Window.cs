#pragma warning disable CS8618 //Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using GLAV;

using Voxand.Content;
using Voxand.Engine.ExecutionControl;
using Voxand.Helpers;
using Voxand.Helpers.Interop;
using DisposableExt;
using Voxand.Engine.Systems.General.ImGuiIntegration;

namespace Voxand;
public sealed class Window : GameWindow
{
    public ExecutionManager ExecutionManager { get; private set; }
    public ContentManager Content { get; private set; }
    public ImGuiBackend ImGuiBackend { get; private set; }
    public bool IsMinimized { get; private set; }

    public Window(GameWindowSettings windowSettings, NativeWindowSettings nativeWindowSettings, ExecutionManager executionManager)
        : base(windowSettings, nativeWindowSettings)
    {
        ExecutionManager = executionManager ?? throw new ArgumentNullException(nameof(executionManager));
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

        Content = new ContentManager("Resources");

        ImGuiBackend = new ImGuiBackend(this, Content);

        ExecutionManager.Load(this);
    }

    protected override void OnUnload()
    {
        ExecutionManager.Unload();
        ImGuiBackend.Dispose();
        base.OnUnload();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        ExecutionManager.Update(args);
        ImGuiBackend.Update();
        base.OnUpdateFrame(args);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GLRegistry.Instance.ProcessOpenGLActions();
        ExecutionManager.Render(args);
        ImGuiBackend.Render();
        Context.SwapBuffers();
        base.OnRenderFrame(args);
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
        GL.Viewport(0, 0, args.Width, args.Height);
        ExecutionManager.OnResize(args);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        ImGuiBackend.PressChar((uint)e.Unicode);
        base.OnTextInput(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ImGuiBackend.MouseScroll(e.Offset);
        base.OnMouseWheel(e);
    }

    protected override void OnMinimized(MinimizedEventArgs e)
    {
        IsMinimized = e.IsMinimized;
        base.OnMinimized(e);
    }
}
