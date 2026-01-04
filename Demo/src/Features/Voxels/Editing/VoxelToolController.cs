using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Scripts;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers.ExtensionMethods;
using Voxand.Helpers.UtilityTypes;
using Voxand.Helpers;
using Voxand.Engine.Systems.Services.General;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.Services.UI.Windows;
using Voxand.Engine.Systems.General.ImGuiIntegration;

using Window = Voxand.Window;
using Sparkvox.src.Services;

namespace Sparkvox.src.Features.Voxels.Editing;
public class VoxelToolController : Script
{
    public required VoxelTool VoxelTool { private get; init; }
    public required Camera Camera { private get; init; }

    public bool AutomaticMode { get; set; } = false;

    float automaticModeDelay = 0.1f;
    public float AutomaticModeDelay
    {
        get => automaticModeDelay;
        set
        {
            automaticModeDelay = value;
            automaticModeTimer.Interval = value;
        }
    }

    IntervalCounter automaticModeTimer;

    Window win;
    IWindowService windowService;
    IVoxelMap voxelMap;

    public override void Initialize()
    {
        windowService = ServiceLocator.GetService<IWindowService>();
        voxelMap = ServiceLocator.GetService<IVoxelMap>();
        win = windowService.Window;

        automaticModeTimer = new IntervalCounter() { Interval = AutomaticModeDelay };

        ServiceLocator.GetService<IVoxelMaterialSelector>().OnMaterialSelected += (index) => VoxelTool.ActiveMaterialIndex = index;
    }

    public override void Update()
    {
        int useCount = 0;
        if (!(VoxelTool.ActivePlacementTechnique is ComplexPlacementTechnique))
        {
            if (win.MouseState.IsButtonReleased(MouseButton.Left))
            {
                automaticModeTimer.Reset();
            }
            else if (AutomaticMode)
            {
                if (win.MouseState.IsButtonDown(MouseButton.Left))
                {
                    useCount = automaticModeTimer.Tick(FrameTime.Delta);
                }
                if (win.MouseState.IsButtonPressed(MouseButton.Left))
                {
                    useCount = Math.Max(1, useCount);
                }
            }

            if (!AutomaticMode)
            {
                useCount = win.MouseState.IsButtonPressed(MouseButton.Left) ? 1 : 0;
            }
        }
        else
        {
            useCount = win.MouseState.IsButtonPressed(MouseButton.Left) ? 1 : 0;
        }



        if (useCount > 0 && !ImGui.GetIO().WantCaptureMouse && !ImGuiInput.IsInteractingWithUI())
        {
            for (int i = 0; i < useCount; i++)
            {
                Vector3 raycastDir = Camera.PixelToRay(
                        uv: win.MouseState.Position / win.ClientSize,
                        aspectRatio: win.ClientSize.Ratio());

                RaycastResult result = voxelMap.RawStructure.Raycast(Camera.Pose.position, raycastDir);

                if (result.hit)
                {
                    VoxelTool.Use(result);
                }
            }
        }
        

        if (win.KeyboardState.IsKeyPressed(Keys.Backspace))
            VoxelTool.Cancel();

        if (win.KeyboardState.IsKeyPressed(Keys.Enter))
            VoxelTool.Apply();
    }

}