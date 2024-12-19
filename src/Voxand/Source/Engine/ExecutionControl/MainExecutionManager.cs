using System.Diagnostics;

using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using DisposableExt;

using Voxand.Helpers;
using Voxand.Helpers.UtilityObjects;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP;
using Voxand.Engine.Systems.Voxels;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.UI;
using Voxand.App;
using Voxand.App.VoxelEditing;

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

    public VoxelBrickmap VoxelMap { get => voxelMap; set { voxelMap = value; Events.Invoke("main_voxelMap_changed", VoxelMap); } }
    VoxelBrickmap voxelMap;

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
public interface IInputState
{
    public KeyboardState KeyboardState { get; }
    public MouseState MouseState { get; }
}
public interface IEngineState
{
    public EventDispatcher Events { get; }
    public VoxelPTRP RenderingPipeline { get; }
    public VoxelBrickmap VoxelMap { get; }
    public VoxelPalette VoxelPalette { get; }
    public Camera MainCamera { get; }
}


public interface IWindowStateSupported
{
    public IWindowState WindowState { get; }
}
public interface IObjectRegistrySupported
{
    public IObjectRegistry ObjectRegistry { get; }
}
public interface IEngineStateSupported
{
    public IEngineState EngineState { get; }
}
public sealed class MainExecutionManager : ExecutionManager,
    IWindowStateSupported, IObjectRegistrySupported, IEngineStateSupported
{
    Vector3i mapSize = new(256, 256, 256);
    Framewatch fps;

    
    readonly EngineState engineState;
    readonly WindowState windowState;
    readonly ObjectRegistry objectRegistry;
    public IWindowState WindowState => windowState;
    public IEngineState EngineState => engineState;
    public IObjectRegistry ObjectRegistry => objectRegistry;

    Viewer viewer;

    public MainExecutionManager(Window win) : base(win)
    {
        windowState = new WindowState(win);
        engineState = new();
        objectRegistry = new();
    }
    
    public override void Load()
    {
        GL.ClearColor(0.6f, 0.3f, 0.2f, 1);

        engineState.MainCamera = new Camera();
        engineState.MainCamera.position = (Vector3)mapSize / 2 + new Vector3(-3, 3, 3);
        engineState.MainCamera.FOV = 90;
        
        VoxelMaterial[] materials =
        {
            new(new(0.8f, 0.8f, 0.8f), new(0, 0, 0)),
            new(Util.Hex2Vec("#376e47"), new(0, 0, 0)),
            new(Util.Hex2Vec("#f5f2a6"), new(0, 0, 0)),
            new(Util.Hex2Vec("#78664e"), new(0, 0, 0)),

            new(new(0.8f, 0.8f, 0.8f), new(2, 1.85f, 1)),
            new(new(0.8f, 0.8f, 0.8f), new(1, 0, 0)),
            new(new(0.8f, 0.8f, 0.8f), new(0, 1, 0)),
            new(new(0.8f, 0.8f, 0.8f), new(0, 0, 1)),
        };
        engineState.VoxelPalette = new(materials, 2);

        engineState.VoxelMap = new(mapSize, new VoxelBrickmapDefaultPersistenceModule());

        UI_Manager.Initialize(main.Content);

        UI_PaletteWindow paletteWindow = new(engineState.VoxelPalette);
        UI_MaterialEditorWindow materialEditorWindow = new(engineState.VoxelPalette);
        UI_SettingsWindow renderSettingsWindow = new();
        UI_DebugWindow debugWindow = new(engineState.VoxelMap);

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
        UI_Manager.Events.Subscribe("voxel_material_selected", (args) =>
        {
            viewer.VoxelTool.ActiveMaterial = (int)args;
        });

        fps = new Framewatch(Util.FrameTimeData, 1);

        engineState.VoxelMap.SetVoxelValueAndBit(engineState.VoxelMap.Dimensions / 2, 0, true);

        engineState.RenderingPipeline = new(
            content: main.Content,
            camera: engineState.MainCamera,
            map: engineState.VoxelMap,
            output: DefaultRenderTarget.Instance,
            renderingResolution: new((main.ClientSize.X / 2) & ~7, (main.ClientSize.Y / 2) & ~7));

        objectRegistry.AddObjects(viewer, voxelTool);

        viewer.VoxelTool.Events.ExportEvents(materialEditorWindow);
        viewer.VoxelTool.Events.ExportEvents(paletteWindow);

        Console.WriteLine("Loaded");
    }
    public override void Update(FrameEventArgs args)
    {
        objectRegistry.Update(args);
        fps.Tick(args);
    }
    public override void Render(FrameEventArgs args)
    {
        engineState.RenderingPipeline.Execute();

        UI_Manager.Display();
    }
    public override void Unload()
    {
        engineState.RenderingPipeline.Dispose();
        engineState.VoxelMap.Dispose();
        engineState.VoxelPalette.Dispose();
        objectRegistry.Dispose();

        Console.WriteLine("Resources was successfully unloaded");
    }
    public override void OnResize(ResizeEventArgs args)
    {
        engineState.RenderingPipeline.SetRenderingResolution(new((args.Size.X / 2) & ~7, (args.Size.Y / 2) & ~7));
    }
}