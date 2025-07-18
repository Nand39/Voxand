#pragma warning disable CS8618 //Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using GLAV;

using Voxand.Content;
using Voxand.Engine.ExecutionControl;
using Voxand.Helpers;
using DisposableExt;
using Voxand.Engine.Systems.General.ImGuiIntegration;
using Voxand.Engine.Systems.Graphics.Tools.Utility;
using Voxand.Helpers.Interop.Win32;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text;

namespace Voxand;
public sealed class Window : GameWindow
{
    public ExecutionManager ExecutionManager { get; private set; }
    public ContentManager Content { get; private set; }
    public ImGuiBackend ImGuiBackend { get; private set; }
    public GLRegistry GLRegistry { get; private set; }
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
        GLRegistry = GLRegistry.Instance;
        
        CenterWindow();
        IsVisible = true;
        Util.ClientSize = ClientSize;

        base.OnLoad();

        string? AsmName = Assembly.GetExecutingAssembly().GetName().Name;

        Content = new ContentManager("Resources");
        ImGuiBackend = new ImGuiBackend(this, Content);

        ComputeUtility.Initialize(Content, ("Voxand.Resources.Graphics.Shaders.copy_tex8_shader.comp", true));

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
    unsafe void OnException(object sender, UnhandledExceptionEventArgs args)
    {
        Console.Error.WriteLine("oops :/");
        Exception ex = (args.ExceptionObject as Exception)!;

        StringBuilder errorMessage = new StringBuilder();
        errorMessage.AppendLine($"Voxand encountered a critical error.");
        errorMessage.AppendLine("");
        errorMessage.AppendLine(ex.Message);
        errorMessage.AppendLine("");
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            errorMessage.AppendLine("Stack trace:");
            errorMessage.AppendLine(ex.StackTrace);
        }
        else
        {
            errorMessage.AppendLine("No stack trace available.");
        }

        nint win32Window = GLFW.GetWin32Window(WindowPtr);

        Win32.MessageBox(win32Window, errorMessage.ToString(), "oops", 0x00000010u);
        
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
