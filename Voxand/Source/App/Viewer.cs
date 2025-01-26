using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.App.VoxelEditing;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.App;
public class Viewer : BaseObject
{
    public Camera Camera { get; set; }
    public VoxelTool VoxelTool { get; set; }

    public bool lockMovement;
    public bool fastMovement;
    public bool cameraFollow;
    public bool hideCursorWhenRotatingCamera;
    public float sensitivity = 0.0038f;
    float speed = 8;
    Vector2i screenCenter;
    int chunkLoadingDistance = 42;

    public event Action? recreateMapRequest;
    public override void Initialize()
    {
        Win.Resize += (args) => screenCenter = args.Size / 2;
        HandleMapLoading();
    }
    public override void Update(FrameEventArgs args)
    {
        if (Win.KeyboardState.IsKeyPressed(Keys.LeftControl))
            fastMovement = !fastMovement;

        if (Win.KeyboardState.IsKeyPressed(Keys.L))
            lockMovement = !lockMovement;

        bool moved = HandleMovement((float)args.Time);
        
        if (moved)
            HandleMapLoading();

        HandleCameraMouseFollow();

        if (Win.KeyboardState.IsKeyPressed(Keys.X))
            VoxelTool.NextTechnique();
        if (Win.KeyboardState.IsKeyPressed(Keys.Z))
            VoxelTool.PreviousTechnique();

        HandleBuilding();

        if (Win.KeyboardState.IsKeyPressed(Keys.R))
        {
            (uint value, ulong bit, int brickIndex) = EngineState.VoxelMap.RawStructure.Examine((Vector3i)Camera.position, false);
            Console.WriteLine($"! CPU SIDE: voxel data at {(Vector3i)Camera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
            (value, bit, brickIndex) = EngineState.VoxelMap.RawStructure.Examine((Vector3i)Camera.position, true);
            Console.WriteLine($"* GPU SIDE: voxel data at {(Vector3i)Camera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
        }
    }
    bool HandleMovement(float deltaTime)
    {
        if (!lockMovement)
        {
            float speed = this.speed * deltaTime;
            Vector3 forward, tangent; Vector3 movement = Vector3.Zero;

            forward = Util.RotateY(Vector3.UnitZ, Camera.rotation.Y);
            tangent = Vector3.Cross(Vector3.UnitY, forward);

            if (Win.KeyboardState.IsKeyDown(Keys.W))
            {
                movement.Z--;
            }
            if (Win.KeyboardState.IsKeyDown(Keys.S))
            {
                movement.Z++;
            }
            if (Win.KeyboardState.IsKeyDown(Keys.A))
            {
                movement.X--;
            }
            if (Win.KeyboardState.IsKeyDown(Keys.D))
            {
                movement.X++;
            }
            if (Win.KeyboardState.IsKeyDown(Keys.Space))
            {
                movement.Y++;
            }
            if (Win.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                movement.Y--;
            }
            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                if (fastMovement)
                    movement *= 8;
                Camera.position += forward * movement.Z * speed;
                Camera.position += tangent * movement.X * speed;
                Camera.position += Vector3.UnitY * movement.Y * speed;
                Camera.position = Vector3.Clamp(Camera.position, Vector3.Zero, new(EngineState.VoxelMap.Dimensions.X - 1, EngineState.VoxelMap.Dimensions.Y - 1, EngineState.VoxelMap.Dimensions.Z - 1));
                return true;
            }
        }
        return false;
    }
    void HandleCameraMouseFollow()
    {
        if (Win.KeyboardState.IsKeyPressed(Keys.E))
        {
            if (cameraFollow)
            {
                cameraFollow = false;
                if (hideCursorWhenRotatingCamera)
                    WindowState.CursorMode = CursorModeValue.CursorNormal;
                    
            }
            else
            {
                Win.MousePosition = screenCenter;
                cameraFollow = true;
                if (hideCursorWhenRotatingCamera)
                    WindowState.CursorMode = CursorModeValue.CursorHidden;
            }
        }
        else if (cameraFollow)
        {
            Vector2 mouseShift = (Win.MousePosition - screenCenter) * sensitivity;
            Win.MousePosition = screenCenter;

            if (mouseShift.X != 0)
            {
                Camera.rotation.Y += mouseShift.X;
                Camera.rotation.Y %= MathF.Tau;
            }
            if (mouseShift.Y != 0)
            {
                Camera.rotation.X += mouseShift.Y;
                Camera.rotation.X = Math.Clamp(Camera.rotation.X, -MathF.PI / 2, MathF.PI / 2);
            }
        }
    }
    void HandleBuilding()
    {
        if (Win.MouseState.IsButtonPressed(MouseButton.Left))
        {
            if (!ImGui.IsAnyItemHovered() && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.None | ImGuiHoveredFlags.RootWindow))
            {
                Vector3 raycastDir = Camera.PixelToRay(
                    uv: Win.MouseState.Position / Win.ClientSize,
                    aspectRatio: Win.ClientSize.Ratio());

                RaycastResult result = EngineState.VoxelMap.RawStructure.Raycast(Camera.position, raycastDir);

                if (result.hit)
                {
                    VoxelTool.Use(result);
                }
            }
        }
    }
    void HandleMapLoading()
    {
        Vector2i camChunk = new((int)Camera.position.X >> 2, (int)Camera.position.Z >> 2);
        Vector2i start = Vector2i.Clamp(new(camChunk.X - chunkLoadingDistance, camChunk.Y - chunkLoadingDistance), Vector2i.Zero, EngineState.VoxelMap.Dimensions.Xz / 4);
        Vector2i finish = Vector2i.Clamp(new(camChunk.X + chunkLoadingDistance + 1, camChunk.Y + chunkLoadingDistance + 1), Vector2i.Zero, EngineState.VoxelMap.Dimensions.Xz / 4);
        Vector2i chunk;
        int chunkLoadingDistanceSquared = chunkLoadingDistance * chunkLoadingDistance;

        for (chunk.Y = start.Y; chunk.Y < finish.Y; chunk.Y++)
        {
            for (chunk.X = start.X; chunk.X < finish.X; chunk.X++)
            {
                if ((chunk - camChunk).EuclideanLengthSquared <= chunkLoadingDistanceSquared)
                EngineState.VoxelMap.StartLoadingChunkIfUnloaded(chunk);
            }
        }
    }
}