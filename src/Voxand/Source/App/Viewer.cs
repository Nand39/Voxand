using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Voxand.App.VoxelEditing;
using Voxand.Engine.ExecutionControl;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;

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
    float speed = 14;
    Vector2i screenCenter;
    public override void Initialize()
    {
        Win.Resize += (args) => screenCenter = args.Size / 2;
    }
    public override void Update(FrameEventArgs args)
    {
        if (Win.KeyboardState.IsKeyPressed(Keys.LeftControl))
            fastMovement = !fastMovement;

        if (Win.KeyboardState.IsKeyPressed(Keys.L))
            lockMovement = !lockMovement;

        HandleMovement((float)args.Time);
        HandleCameraMouseFollow();

        bool materialIncremented = Win.KeyboardState.IsKeyPressed(Keys.Z);
        bool materialDecremented = Win.KeyboardState.IsKeyPressed(Keys.X);

        if (materialIncremented || materialDecremented)
        {
            if (materialIncremented != materialDecremented)
            {
                int materialChange = materialIncremented ? 1 : -1;
                VoxelTool.ActiveMaterial = (VoxelTool.ActiveMaterial + materialChange) % EngineState.VoxelPalette.MaterialCount;
            }
        }

        HandleBuilding();

        if (Win.KeyboardState.IsKeyPressed(Keys.R))
        {
            (uint value, uint bit, int brickIndex) = EngineState.VoxelMap.Examine((Vector3i)Camera.position, false);
            Console.WriteLine($"! CPU SIDE: voxel data at {(Vector3i)Camera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
            (value, bit, brickIndex) = EngineState.VoxelMap.Examine((Vector3i)Camera.position, true);
            Console.WriteLine($"* GPU SIDE: voxel data at {(Vector3i)Camera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
        }
    }
    void HandleMovement(float deltaTime)
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
            }

            Camera.position = Vector3.Clamp(Camera.position, Vector3.Zero, new(EngineState.VoxelMap.Dimensions.X - 1, EngineState.VoxelMap.Dimensions.Y - 1, EngineState.VoxelMap.Dimensions.Z - 1));
        }
    }
    void HandleBuilding()
    {
        if (Win.KeyboardState.IsKeyPressed(Keys.B))
            VoxelTool.PlaceSingle((Vector3i)Camera.position);

        if (!ImGui.IsAnyItemHovered() && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.None | ImGuiHoveredFlags.RootWindow))
        {
            bool placeVoxels = Win.MouseState.IsButtonPressed(MouseButton.Right);
            bool removeVoxels = Win.MouseState.IsButtonPressed(MouseButton.Left);

            if (placeVoxels || removeVoxels)
            {
                Vector3 raycastDir = Camera.PixelToRay(
                    uv: Win.MouseState.Position / Win.ClientSize,
                    aspectRatio: (float)Win.ClientSize.X / Win.ClientSize.Y);

                DDAOut result = EngineState.VoxelMap.Raycast(Camera.position, raycastDir);

                if (result.hit)
                {
                    if (placeVoxels)
                        VoxelTool.PlaceSingle(
                            result.voxelHitPos + (Vector3i)Util.RotateUnitYByNormalIndex(Vector3.UnitY, result.normal));
                    else
                        VoxelTool.RemoveSingle(result.voxelHitPos);
                }
            }
        }
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
}