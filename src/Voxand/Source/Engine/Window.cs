#pragma warning disable CS8618 //Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Voxand.Content;
using Voxand.Engine.GameStates;
using Voxand.Engine;
using System.Reflection;
using Voxand.Helpers;
using Voxand.UI;
using Voxand.Helpers.Interop;
using GLAV.Systems;

namespace Voxand;
public class Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    GameState gameState;
    public ContentManager content;
    public ImGuiController imGui;

    protected override void OnLoad()
    {
        AppDomain.CurrentDomain.UnhandledException += OnException;
        GLRegistry.Initialize(Context);

        CenterWindow();
        IsVisible = true;
        Util.ClientSize = ClientSize;

        base.OnLoad();

        string? AsmName = Assembly.GetExecutingAssembly().GetName().Name;

        content = new ContentManager(
            basePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content"), 
            asmBasePath: string.Join('.', AsmName, "Content"));

        imGui = new ImGuiController(ClientSize.X, ClientSize.Y);

        SetGameState(new ActiveState(this));

        MouseWheel += (args) =>
        {
            imGui.MouseScroll(args.Offset);
        };
    }
    protected override void OnUnload()
    {
        gameState.Unload();
        base.OnUnload();
    }
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        gameState.Update(args);
        imGui.Update(this, (float)args.Time);
        base.OnUpdateFrame(args);
    }
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GLRegistry.Instance.ProcessOpenGLActions();
        gameState.Render(args);
        imGui.Render();
        Context.SwapBuffers();
        base.OnRenderFrame(args);
    }

    //======================================================
    //additional functionality
    //======================================================
    
    public void SetGameState(GameState newState)
    {
        if (newState == null)
        {
            Close();
            throw new ArgumentNullException("GameState cannot be set to null");
        }

        if (gameState != null) gameState.Unload();

        gameState = newState;
        gameState.Load();
    }
    void OnException(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine("oops :/");
        Exception ex = (args.ExceptionObject as Exception)!;
        NativeFuncs.Win.MessageBox(0, $"Voxand has crashed lmao\n{ex.Message}\nStack trace:\n{ex.StackTrace ?? "! bad luck"}", "oops", 0x00000000u);
        Close();
        Environment.Exit(ex.HResult);
    }
    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        Util.ClientSize = ClientSize;
        imGui.WindowResized(ClientSize.X, ClientSize.Y);
        GL.Viewport(0, 0, args.Width, args.Height);
        gameState.OnResize(args);
    }
}
