using System.Runtime.InteropServices;
using System.Text;

using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using DisposableExt;

using Voxand.Helpers;
using Voxand.Helpers.UtilityObjects;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP;
using Voxand.Engine.Systems.Graphics.Tools.Utility;
using Voxand.Engine.Systems.Voxels;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.UI;
using Voxand.App;
using Voxand.App.VoxelEditing;
using Voxand.App.Map.Generation;
using Voxand.App.Map;

namespace Voxand.Engine.ExecutionControl;

public class WindowState(Window window) : IWindowState
{
    public Window Window { get; set; } = window;
    public unsafe CursorModeValue CursorMode
    {
        get => cursorMode;
        set
        {
            cursorMode = value;
            GLFW.SetInputMode(Window.WindowPtr, CursorStateAttribute.Cursor, cursorMode);
        }
    }
    CursorModeValue cursorMode;
}
public class EngineState : IEngineState
{
    public EngineState()
    {
        Events.AddEvent("main_renderingPipeline_changed");
        Events.AddEvent("main_voxelMap_changed");
        Events.AddEvent("main_voxelPalette_changed");
        Events.AddEvent("main_camera_changed");
    }

    public EventDispatcher Events { get; set; } = new("engine_state_events");

    public VoxelPTRP RenderingPipeline { get => renderingPipeline; set { renderingPipeline = value; Events.Invoke("main_renderingPipeline_changed", RenderingPipeline); } }
    VoxelPTRP renderingPipeline;

    public ChunkMap VoxelMap { get => voxelMap; set { voxelMap = value; Events.Invoke("main_voxelMap_changed", VoxelMap); } }
    ChunkMap voxelMap;

    public VoxelPalette VoxelPalette { get => voxelPalette; set { voxelPalette = value; Events.Invoke("main_voxelPalette_changed", VoxelPalette); } }
    VoxelPalette voxelPalette;

    public Camera MainCamera { get => mainCamera; set { mainCamera = value; Events.Invoke("main_camera_changed", MainCamera); } }
    Camera mainCamera;
}
public class ObjectRegistry : IObjectRegistry, IDisposableExt
{
    List<BaseObject> objects = [];
    Dictionary<BaseObject, int> indexMap = [];

    public DisposeHelper DisposeHelper { get; }
    public ObjectRegistry() => DisposeHelper = new(this);

    public void AddObject(BaseObject objectToAdd)
    {
        ArgumentNullException.ThrowIfNull(objectToAdd);
        objectToAdd.Initialize();
        indexMap[objectToAdd] = objects.Count;
        objects.Add(objectToAdd);
    }
    public void AddObjects(params BaseObject[] objects)
    {
        foreach (var obj in objects)
            AddObject(obj);
    }
    public void DeleteObject(BaseObject objectToDelete)
    {
        ArgumentNullException.ThrowIfNull(objectToDelete);
        if (indexMap.TryGetValue(objectToDelete, out int index))
        {
            objects[index] = objects[objects.Count - 1];
            objects[objects.Count - 1] = null;
            indexMap.Remove(objectToDelete);
            objectToDelete.Dispose();
            return;
        }
        throw new KeyNotFoundException($"Cannot remove object from the registry. Object not found.");
    }
    public void DeleteObjects(params BaseObject[] objects)
    {
        foreach (var obj in objects)
            DeleteObject(obj);
    }
    public void Update(FrameEventArgs args)
    {
        for (int i = 0; i < objects.Count; i++)
            objects[i].Update(args);
    }
    void IDisposableExt.Free()
    {
        for (int i = 0; i < objects.Count; i++)
            DeleteObject(objects[0]);
    }
}

public interface IWindowState
{
    public Window Window { get; }
    public CursorModeValue CursorMode { get; set; }
}
public interface IObjectRegistry
{
    public void AddObject(BaseObject objectToAdd);
    public void AddObjects(params BaseObject[] objectToAdd);
}
public interface IEngineState
{
    public EventDispatcher Events { get; }
    public VoxelPTRP RenderingPipeline { get; }
    public ChunkMap VoxelMap { get; }
    public VoxelPalette VoxelPalette { get; }
    public Camera MainCamera { get; }
}

public interface ISupportsWindowState
{
    public IWindowState WindowState { get; }
}
public interface ISupportsObjectRegistry
{
    public IObjectRegistry ObjectRegistry { get; }
}
public interface ISupportsEngineState
{
    public IEngineState EngineState { get; }
}

public sealed class MainExecutionManager : ExecutionManager,
    ISupportsWindowState, ISupportsObjectRegistry, ISupportsEngineState
{
    Vector3i numberOfChunks = new(128, 42, 128);
    Framewatch fps;
    
    readonly EngineState engineState;
    readonly WindowState windowState;
    readonly ObjectRegistry objectRegistry;
    public IWindowState WindowState => windowState;
    public IEngineState EngineState => engineState;
    public IObjectRegistry ObjectRegistry => objectRegistry;

    Viewer viewer;
    DebugProc debugCallback;
    public MainExecutionManager(Window win)
    {
        windowState = new WindowState(win);
        engineState = new();
        objectRegistry = new();
    }
    
    public override void Load()
    {
        debugCallback = GLDebugCallback;
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(debugCallback, nint.Zero);

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1);

        ComputeUtility.Initialize(windowState.Window.Content, "Graphics/Shaders/copy_tex8_shader.comp");

        engineState.MainCamera = new Camera();
        engineState.MainCamera.position = new Vector3(numberOfChunks.X * 2, numberOfChunks.Y * 3.7f, numberOfChunks.Z * 2);
        engineState.MainCamera.FOV = 90 * Util.DEG2RAD;
        engineState.MainCamera.rotation = new(0, 45, 0);

        VoxelMaterial[] materials =
        {
            new(Util.Hex2Vec("#50555c"), new(0, 0, 0)),
            new(Util.Hex2Vec("#5e6269"), new(0, 0, 0)),
            new(Util.Hex2Vec("#42454a"), new(0, 0, 0)),

            new(Util.Hex2Vec("#614c31"), new(0, 0, 0)),
            new(Util.Hex2Vec("#735b3d"), new(0, 0, 0)),
            new(Util.Hex2Vec("#52422e"), new(0, 0, 0)),

            new(Util.Hex2Vec("#376e47"), new(0, 0, 0)),
            new(Util.Hex2Vec("#3e8051"), new(0, 0, 0)),
            new(Util.Hex2Vec("#326b43"), new(0, 0, 0)),

            new(new(0.8f, 0.8f, 0.8f), new(2, 1.85f, 1)),
            new(new(0.8f, 0.8f, 0.8f), new(1, 0, 0)),
            new(new(0.8f, 0.8f, 0.8f), new(0, 1, 0)),
            new(new(0.8f, 0.8f, 0.8f), new(0, 0, 1)),
        };

        engineState.VoxelPalette = new(materials, 2);

        engineState.VoxelMap = new(numberOfChunks.Xz, new(4), numberOfChunks.Y * 4);
        engineState.VoxelMap.MapGenerator = new BrickmapGenerator(engineState.VoxelMap.RawStructure, new(200, 200), 120);

        UI_Manager.Initialize(WindowState.Window.Content);

        UI_PaletteWindow paletteWindow = new(engineState.VoxelPalette);
        UI_MaterialEditorWindow materialEditorWindow = new(engineState.VoxelPalette);
        UI_SettingsWindow renderSettingsWindow = new();
        UI_DebugWindow debugWindow = new(engineState.VoxelMap.RawStructure);

        paletteWindow.Select(0);

        VoxelTool voxelTool = new();
        viewer = new()
        {
            VoxelTool = voxelTool,
            Camera = engineState.MainCamera
        };

        engineState.Events.ExportEvents(materialEditorWindow);
        engineState.Events.ExportEvents(paletteWindow);
        engineState.Events.ExportEvents(debugWindow);
        engineState.VoxelPalette.Events.ExportEvents(paletteWindow);

        UI_Manager.AddWindow(materialEditorWindow);
        UI_Manager.AddWindow(paletteWindow);
        UI_Manager.AddWindow(renderSettingsWindow);
        UI_Manager.AddWindow(debugWindow);

        UI_Manager.Events.Subscribe("voxel_material_edited", engineState.VoxelPalette.SetMaterial);
        UI_Manager.Events.Subscribe("voxel_material_selected", (args) => { viewer.VoxelTool.ActiveMaterial = (int)args; });
        UI_Manager.Events.Subscribe("renderSettings_TAA_switched", (args) => 
        {
            engineState.RenderingPipeline.UseTAA = (bool)args;
            engineState.RenderingPipeline.antiAliasingSettings.ResetAccumulated(); 
        });

        fps = new Framewatch(Util.FrameTimeData, 1);

        engineState.RenderingPipeline = new(
            content: WindowState.Window.Content,
            camera: engineState.MainCamera,
            map: engineState.VoxelMap,
            output: DefaultRenderTarget.Instance,
            renderingResolution: new((windowState.Window.ClientSize.X / 2) & ~7, (windowState.Window.ClientSize.Y / 2) & ~7));

        objectRegistry.AddObjects(viewer, voxelTool);

        viewer.VoxelTool.Events.ExportEvents(materialEditorWindow);
        viewer.VoxelTool.Events.ExportEvents(paletteWindow);

        viewer.recreateMapRequest += () =>
        {
            engineState.VoxelMap.Dispose();
            engineState.VoxelMap = new(numberOfChunks.Xz, new(4), numberOfChunks.Y * 4);
        };

        engineState.RenderingPipeline.antiAliasingSettings.Intensity = 0.95f;

        Console.WriteLine("Loaded");
    }
    public override void Update(FrameEventArgs args)
    {
        objectRegistry.Update(args);
        fps.Tick(args);
    }
    public override void Render(FrameEventArgs args)
    {
        if (!WindowState.Window.IsMinimized)
            engineState.RenderingPipeline.Execute();

        UI_Manager.Display();
    }
    public override void Unload()
    {
        engineState.RenderingPipeline.Dispose();
        engineState.VoxelMap.Dispose();
        engineState.VoxelPalette.Dispose();
        objectRegistry.Dispose();

        Console.WriteLine("Unloaded");
    }
    public override void OnResize(ResizeEventArgs args)
    {
        engineState.RenderingPipeline.SetRenderingResolution(new((args.Size.X) & ~7, (args.Size.Y) & ~7));
    }
    void GLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int messageLength, nint messagePtr, nint userParamPtr)
    {
        byte[] messageBytes = new byte[messageLength];
        Marshal.Copy(messagePtr, messageBytes, 0, messageLength);
        string message = Encoding.UTF8.GetString(messageBytes);
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = color;
        Console.Beep();
    }
}