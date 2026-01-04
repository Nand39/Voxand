using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sparkvox.src.Features.Voxels.Editing;
using Sparkvox.src.Features.Voxels.Map;
using Sparkvox.src.Services;
using Voxand.Engine.Systems.General.ImGuiIntegration;
using Voxand.Engine.Systems.Scripts;
using Voxand.Engine.Systems.Services.General;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods;
using Voxand.Helpers.UtilityTypes;
using Window = Voxand.Window;

namespace Sparkvox.src.Features;

public class Viewer : Script
{
    public required ICamera Camera { get; set; }
    public required VoxelTool VoxelTool { get; set; }
    public required VoxelToolHotbar VoxelToolHotbar { get; set; }

    public bool lockMovement;
    public bool fastMovement;
    public bool cameraFollow;
    public bool hideCursorWhenRotatingCamera = true;
    public float sensitivity = 0.0038f;
    float speed = 15;
    Vector2i screenCenter;
    int chunkLoadingDistance = 16;

    IntervalCounter accumulatedScroll;
    int scrollSign = 0;

    Window win;
    IWindowService windowService;
    IVoxelMap voxelMap;
    IVoxelMapPersistence voxelMapPersistence;
    IVoxelMapVerticalChunks voxelMapChunks;
    IVoxelPalette palette;

    public event Action? recreateMapRequest;
    public event Action<Vector3>? setSunDirectionRequest;
    public override void Initialize()
    {
        windowService = ServiceLocator.GetService<IWindowService>();
        voxelMap = ServiceLocator.GetService<IVoxelMap>();
        voxelMapChunks = ServiceLocator.GetService<IVoxelMapVerticalChunks>();
        palette = ServiceLocator.GetService<IVoxelPalette>();
        voxelMapPersistence = ServiceLocator.GetService<IVoxelMapPersistence>();

        win = windowService.Window;

        win.Resize += (args) => screenCenter = args.Size / 2;
        HandleMapLoading();

        ServiceLocator.AddReplacementCallback<ICamera>((camera) => Camera = camera);
        ServiceLocator.AddReplacementCallback<IVoxelMap>((map) => voxelMap = map);
        ServiceLocator.AddReplacementCallback<IVoxelMapVerticalChunks>((chunkMap) => voxelMapChunks = chunkMap);
        ServiceLocator.AddReplacementCallback<IVoxelPalette>((palette) => this.palette = palette);
        ServiceLocator.AddReplacementCallback<IVoxelMapPersistence>((persistance) => voxelMapPersistence = persistance);

        accumulatedScroll = new() { Interval = 0.999f }; // Account for imprecision

        win.MouseWheel += (args) =>
        {
            if (ImGuiInput.IsInteractingWithUI())
                return;


            int newScrollSign = Math.Sign(args.OffsetY);
            if (newScrollSign != scrollSign)
            {
                accumulatedScroll.ElapsedTime = -accumulatedScroll.ElapsedTime;
                scrollSign = newScrollSign;
            }
            int scrolled = accumulatedScroll.Tick(Math.Abs(args.Offset.Y));
            if (accumulatedScroll.ElapsedTime < 0.01) // Account for accountment for imprecision
                accumulatedScroll.Reset();

            int materialIndex = VoxelTool.ActiveMaterialIndex + (newScrollSign > 0 ? scrolled : -scrolled);
            VoxelTool.ActiveMaterialIndex = (materialIndex % palette.MaterialCount + palette.MaterialCount) % palette.MaterialCount;
        };
    }
    public override void Update()
    {
        if (win.KeyboardState.IsKeyPressed(Keys.LeftControl) || win.KeyboardState.IsKeyPressed(Keys.RightControl))
            fastMovement = !fastMovement;

        if (win.KeyboardState.IsKeyPressed(Keys.C))
            lockMovement = !lockMovement;

        bool moved = HandleMovement();

        if (moved)
            HandleMapLoading();

        HandleCameraMouseFollow();

        //if (win.KeyboardState.IsKeyPressed(Keys.R))
        //{
        //    Console.WriteLine($"chunk: {((ChunkMap)voxelMap).IsChunkLoaded((Vector2i)(Camera.Pose.position.Xz / ((ChunkMap)voxelMap).ChunkSize))}");
        //    (uint value, ulong bit, VoxelBrickHandle brickHandle) = (voxelMap.RawStructure as VoxelBrickmap)!.Examine((Vector3i)Camera.Pose.position, false);
        //    Console.WriteLine($"! CPU SIDE: voxel data at {(Vector3i)Camera.Pose.position}: value={value}; empty={bit == 0}; brick={(brickHandle.IsEmpty ? brickHandle.DistanceFieldValue : brickHandle.BrickIndex)}");
        //    (value, bit, brickHandle) = (voxelMap.RawStructure as VoxelBrickmap)!.Examine((Vector3i)Camera.Pose.position, true);
        //    Console.WriteLine($"* GPU SIDE: voxel data at {(Vector3i)Camera.Pose.position}: value={value}; empty={bit == 0}; brick={(brickHandle.IsEmpty ? brickHandle.DistanceFieldValue : brickHandle.BrickIndex)}");
        //}

        if (win.KeyboardState.IsKeyPressed(Keys.Q))
        {
            Vector3 sunDir = Camera.PixelToRay(
                uv: win.MouseState.Position / win.ClientSize,
                aspectRatio: win.ClientSize.Ratio());

            setSunDirectionRequest?.Invoke(sunDir);
        }

        if (!ImGui.IsAnyItemActive())
        {
            for (int i = 0; i < 10; i++)
            {
                Keys numKey = i == 9 ? Keys.D0 : Keys.D1 + i;

                if (win.KeyboardState.IsKeyPressed(numKey))
                {
                    int index = VoxelToolHotbar[i];
                    if (index >= 0)
                        VoxelTool.ActivePlacementTechniqueIndex = index;
                }
            }
        }
    }

    bool HandleMovement()
    {
        if (!lockMovement)
        {
            float speed = this.speed * FrameTime.Delta;
            Vector3 forward, tangent; Vector3 movement = Vector3.Zero;

            forward = Util.RotateY(Vector3.UnitZ, Camera.Pose.rotation.Y);
            tangent = Vector3.Cross(Vector3.UnitY, forward);

            if (win.KeyboardState.IsKeyDown(Keys.W))
            {
                movement.Z--;
            }
            if (win.KeyboardState.IsKeyDown(Keys.S))
            {
                movement.Z++;
            }
            if (win.KeyboardState.IsKeyDown(Keys.A))
            {
                movement.X--;
            }
            if (win.KeyboardState.IsKeyDown(Keys.D))
            {
                movement.X++;
            }
            if (win.KeyboardState.IsKeyDown(Keys.Space))
            {
                movement.Y++;
            }
            if (win.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                movement.Y--;
            }
            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                if (fastMovement)
                    movement *= 8;
                Camera.Pose.position += forward * movement.Z * speed;
                Camera.Pose.position += tangent * movement.X * speed;
                Camera.Pose.position += Vector3.UnitY * movement.Y * speed;
                Camera.Pose.position = Vector3.Clamp(Camera.Pose.position, Vector3.Zero, new(voxelMap.Dimensions.X - 1, voxelMap.Dimensions.Y - 1, voxelMap.Dimensions.Z - 1));
                return true;
            }
        }
        return false;
    }

    void HandleCameraMouseFollow()
    {
        if (win.KeyboardState.IsKeyPressed(Keys.E))
        {
            if (cameraFollow)
            {
                cameraFollow = false;
                if (hideCursorWhenRotatingCamera)
                    windowService.CursorMode = CursorModeValue.CursorNormal;

            }
            else
            {
                cameraFollow = true;
                win.MousePosition = screenCenter;
                ImGui.SetWindowFocus(null);
                if (hideCursorWhenRotatingCamera)
                    windowService.CursorMode = CursorModeValue.CursorHidden;
            }
        }
        else if (cameraFollow)
        {
            Vector2 mouseShift = (win.MousePosition - screenCenter) * sensitivity;
            win.MousePosition = screenCenter;

            if (mouseShift.X != 0)
            {

                Camera.Pose.rotation.Y += mouseShift.X;
                Camera.Pose.rotation.Y %= MathF.Tau;
            }
            if (mouseShift.Y != 0)
            {
                Camera.Pose.rotation.X += mouseShift.Y;
                Camera.Pose.rotation.X = Math.Clamp(Camera.Pose.rotation.X, -MathF.PI / 2, MathF.PI / 2);
            }
        }
    }

    void HandleMapLoading()
    {
        Vector2i camChunk = new((int)Camera.Pose.position.X >> 2, (int)Camera.Pose.position.Z >> 2);
        Vector2i start = Vector2i.Clamp(new Vector2i(camChunk.X - chunkLoadingDistance, camChunk.Y - chunkLoadingDistance), Vector2i.Zero, voxelMap.Dimensions.Xz / 4);
        Vector2i finish = Vector2i.Clamp(new Vector2i(camChunk.X + chunkLoadingDistance + 1, camChunk.Y + chunkLoadingDistance + 1), Vector2i.Zero, voxelMap.Dimensions.Xz / 4);
        Vector2i chunk;
        int chunkLoadingDistanceSquared = chunkLoadingDistance * chunkLoadingDistance;

        for (chunk.Y = start.Y; chunk.Y < finish.Y; chunk.Y++)
        {
            for (chunk.X = start.X; chunk.X < finish.X; chunk.X++)
            {
                if ((chunk - camChunk).EuclideanLengthSquared <= chunkLoadingDistanceSquared)
                    voxelMapChunks.StartLoadingChunkIfUnloaded(chunk);
            }
        }
    }
}