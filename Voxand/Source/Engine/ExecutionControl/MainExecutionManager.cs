using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;

using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using Voxand.Helpers;
using Voxand.Helpers.UtilityTypes;
using Voxand.Helpers.ExtensionMethods;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP;
using Voxand.Engine.Systems.Graphics.Tools.Utility;
using Voxand.Engine.Systems.Voxels;
using Voxand.Engine.Systems.Scripts;
using Voxand.UI;
using Voxand.App;
using Voxand.App.Voxels.Editing;
using Voxand.App.Voxels.Map.Generation;
using Voxand.App.Voxels.Map;
using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;
using Voxand.Engine.Systems.Services.General;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.Services.Graphics;
using Voxand.Engine.Systems.Services.UI.Windows;
using Voxand.App.Voxels.Materials;
using Voxand.Engine.Systems.Common;

namespace Voxand.Engine.ExecutionControl;
public sealed class MainExecutionManager : ExecutionManager
{
    Vector3i numberOfChunks = new(128, 48, 128);
    Framewatch fps;
    Action<double> frameTimeSetter;
    
    public WindowService WindowService { get; private set; }
    public ScriptManager ScriptManager { get; private set; }

    VoxelPTRP renderer;
    IRendererPathTracing pathTracing;
    IRendererAntiAliasing antiAliasing;

    Viewer viewer;
    DebugProc debugCallback;

    private interface IServiceRegistrator
    {
        void AddOrReplaceService<I>(I serviceImpl) where I : class;
    }
    public class ServiceRegistry : IServiceRegistrator
    {
        Dictionary<Type, object> serviceMap = [];
        public Dictionary<Type, Action<object>> replacementCallbacks = [];
        void IServiceRegistrator.AddOrReplaceService<I>(I service) where I : class
        {
            if (serviceMap.ContainsKey(typeof(I)))
            {
                object oldService = serviceMap[typeof(I)];
                if (oldService == service)
                    return;
                serviceMap[typeof(I)] = service;
                replacementCallbacks[typeof(I)]?.Invoke(service);
            }
            else
            {
                serviceMap[typeof(I)] = service;
                replacementCallbacks[typeof(I)] = null!;
            }
                
        }

        public bool TryGetService<I>(out I service) where I : class
        {
            
            if (!serviceMap.TryGetValue(typeof(I), out object? serviceObj))
            {
                service = null!;
                return false;
            }
            service = (I)serviceObj;
            return true;
        }
        public I? GetService<I>() where I : class
        {
            if (!serviceMap.TryGetValue(typeof(I), out object? serviceObj))
                return null!;
            return (I)serviceObj;
        }

        public void AddReplacementCallback<I>(Action<object> callback) where I : class
        {
            if (!replacementCallbacks.ContainsKey(typeof(I)))
                throw new InvalidOperationException($"Service of type {typeof(I)} is not registered. Cannot add replacement callback.");

            if (replacementCallbacks[typeof(I)] is not null)
                replacementCallbacks[typeof(I)] += callback;
            else
                replacementCallbacks[typeof(I)] = callback;
        }
    }

    ServiceRegistry services;

    public MainExecutionManager(Window win)
    {
        WindowService = new WindowService(win);
        ScriptManager = new ScriptManager();
        services = new ServiceRegistry();
        EngineServices.Initialize(services);
    }

    public override void Load()
    {
        debugCallback = GLDebugCallback;
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(debugCallback, nint.Zero);

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1);

        WindowService.Window.VSync = VSyncMode.On;

        IServiceRegistrator serviceRegistrator = services;

        serviceRegistrator.AddOrReplaceService<IWindowService>(WindowService);

        Camera camera = new Camera();
        camera.Pose.position = new Vector3(0.5f, numberOfChunks.Y * 3.7f, 0.5f);
        camera.Pose.rotation = new(0, 45, 0);
        camera.FOV = 90 * Util.DEG2RAD;

        serviceRegistrator.AddOrReplaceService<ICamera>(camera);

        VoxelMaterial[] materials = new VoxelMaterial[256];
        materials[0] = new(Util.Hex2Vec("#92959c"), 0.5f, new(0, 0, 0), 0);
        materials[1] = new(Util.Hex2Vec("#614c31"), 0.23f, new(0, 0, 0), 0);
        materials[2] = new(Util.Hex2Vec("#375933"), 0.23f, new(0, 0, 0), 0);
        materials[3] = new(new(0.8f, 0.8f, 0.8f), 0, new(1, 0.93f, 0.5f), 2);
        materials[4] = new(new(0.8f, 0.8f, 0.8f), 0, new(1, 0, 0), 1);
        materials[5] = new(new(0.8f, 0.8f, 0.8f), 0, new(0, 1, 0), 1);
        materials[6] = new(new(0.8f, 0.8f, 0.8f), 0, new(0, 0, 1), 1);

        VoxelPalette palette = new VoxelPalette(materials, 3);
        serviceRegistrator.AddOrReplaceService<IVoxelPalette>(palette);

        VoxelBrickmap brickmap = new(numberOfChunks * 4, new VoxelBrickmapDefaultPersistenceModule());
        BrickmapGenerator mapGenerator = new(brickmap, new(200, 200), 120);
        ChunkMap chunkMap = new ChunkMap(numberOfChunks.Xz, new(4), numberOfChunks.Y * 4, brickmap, mapGenerator);

        serviceRegistrator.AddOrReplaceService<IVoxelMap>(chunkMap);
        serviceRegistrator.AddOrReplaceService<IVoxelMapVerticalChunks>(chunkMap);



        ComputeUtility.Initialize(WindowService.Window.Content, "Graphics/Shaders/copy_tex8_shader.comp");

        UI_Manager.Initialize(WindowService.Window.Content);

        
        
        UI_PaletteWindow paletteWindow = new();
        paletteWindow.SetPalette(palette);
        UI_Manager.AddWindow(paletteWindow);
        serviceRegistrator.AddOrReplaceService<IVoxelMaterialSelector>(paletteWindow);


        UI_MaterialEditorWindow materialEditorWindow = new();
        MaterialEditorController MaterialEditorController = new() { MaterialEditor = materialEditorWindow };

        UI_Manager.AddWindow(materialEditorWindow);



        VoxelTool voxelTool = new();
        VoxelToolController toolController = new() 
        {
            VoxelTool = voxelTool,
            Camera = camera
        };
        VoxelToolHotbar hotbar = new();

        viewer = new()
        {
            VoxelTool = voxelTool,
            VoxelToolHotbar = hotbar,
            Camera = camera
        };
        ScriptManager.AddScripts(viewer, voxelTool, toolController, MaterialEditorController);

        voxelTool.OnMaterialChanged += (index) => MaterialEditorController.EditedMaterialIndex = index;
        voxelTool.OnMaterialChanged += paletteWindow.ActiveMaterialChanged; 

        UI_Manager.AddWindow(new UI_VoxelToolSettingsWindow(voxelTool, toolController));
        UI_Manager.AddWindow(new UI_VoxelToolHotbar(voxelTool, hotbar));

        paletteWindow.Select(0);


        frameTimeSetter = typeof(FrameTime).GetProperty(nameof(FrameTime.PreciseDelta), BindingFlags.Static | BindingFlags.Public)!.GetSetMethod(true)!.CreateDelegate<Action<double>>();

        fps = new Framewatch(Util.FrameTimeAnalytics, 1);

        renderer = new(
            content: WindowService.Window.Content,
            camera: camera,
            map: chunkMap,
            output: RenderTarget.Default,
            renderingResolution: new((WindowService.Window.ClientSize.X / 2) & ~7, (WindowService.Window.ClientSize.Y / 2) & ~7),
            antiAliasing: out antiAliasing,
            pathTracing: out pathTracing);

        serviceRegistrator.AddOrReplaceService<IRendererAntiAliasingUsage>(renderer);
        serviceRegistrator.AddOrReplaceService(pathTracing);
        serviceRegistrator.AddOrReplaceService(antiAliasing);

        EngineServices.AddReplacementCallback<IRendererPathTracing>((newPathTracing) => pathTracing = newPathTracing);
        EngineServices.AddReplacementCallback<IRendererAntiAliasing>((newAntiAliasing) => antiAliasing = newAntiAliasing);
        
        
        UI_SettingsWindow renderSettingsWindow = new();
        UI_DebugWindow debugWindow = new();

        UI_Manager.AddWindow(renderSettingsWindow);
        UI_Manager.AddWindow(debugWindow);

        viewer.recreateMapRequest += () =>
        {
            EngineServices.TryGetService(out IVoxelMap map);
            map.Dispose();
            VoxelBrickmap brickmap = new(numberOfChunks * 4, new VoxelBrickmapDefaultPersistenceModule());
            BrickmapGenerator mapGenerator = new(brickmap, new(200, 200), 120);
            map = new ChunkMap(numberOfChunks.Xz, new(4), numberOfChunks.Y * 4, brickmap, mapGenerator);
            serviceRegistrator.AddOrReplaceService(map);
        };

        viewer.setSunDirectionRequest += (args) => sunDirection = args;
        antiAliasing.Intensity = 0.95f;

        Console.WriteLine("Loaded");
    }

    Vector3 sunDirection = Vector3.UnitX;
    public override void Update(FrameEventArgs args)
    {
        frameTimeSetter(args.Time);

        ScriptManager.Update();

        sunDirection.Xz = sunDirection.Xz.Rotated(0.014f * (float)args.Time);
        pathTracing.SunDirection.Set(sunDirection);

        fps.Tick(args);
    }
    public override void Render(FrameEventArgs args)
    {
        if (!WindowService.Window.IsMinimized)
            renderer.Execute();

        UI_Manager.Display();
    }
    public override void Unload()
    {
        renderer.Dispose();
        EngineServices.TryGetService<IVoxelMap>(out var map);
        EngineServices.TryGetService<IVoxelPalette>(out var palette);
        map.Dispose();
        palette.Dispose();
        ScriptManager.Dispose();

        Console.WriteLine("Unloaded");
    }
    public override void OnResize(ResizeEventArgs args)
    {
        renderer.SetRenderingResolution(new((args.Size.X / 2) & ~7, (args.Size.Y / 2) & ~7));
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