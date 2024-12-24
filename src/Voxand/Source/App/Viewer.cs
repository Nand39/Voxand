using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Voxand.App.VoxelEditing;
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
    float buildingRadius = 4;
    bool isRenderInterupted = false;
    bool wasRenderInterupted = false;
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

        if (isRenderInterupted)
        {
            isRenderInterupted = false;
            wasRenderInterupted = true;
            OnFastRender();
        }
        else if (wasRenderInterupted)
        {
            wasRenderInterupted = false;
            OnIntenseRender();
        }

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

        if (Win.KeyboardState.IsKeyPressed(Keys.Y))
        {
            Vector3[] unitHits = RayDistributionTest(64, Util.Random.NextSingle());
            Vector3i[] voxelHits = new Vector3i[unitHits.Length];
            for (int i = 0; i < unitHits.Length; i++)
            {
                voxelHits[i] = (Vector3i)((unitHits[i] * 40) + EngineState.VoxelMap.Dimensions / 2);
                Console.WriteLine($"unitHit * 40 ={unitHits[i] * 40}; voxelHit = {voxelHits[i]}.");
            }
            for (int i = 0; i < voxelHits.Length; i++)
            {
                VoxelTool.PlaceSingle(voxelHits[i]);
            }
        }
    }
    void OnIntenseRender()
    {
        EngineState.RenderingPipeline.antiAliasingSettings.Intensity = 0.98f;
        EngineState.RenderingPipeline.voxelPathTracingSettings.Samples = 2;
    }
    void OnFastRender()
    {
        EngineState.RenderingPipeline.antiAliasingSettings.Intensity = 0f;
        EngineState.RenderingPipeline.voxelPathTracingSettings.Samples = 1;
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
                isRenderInterupted = true;
            }

            Camera.position = Vector3.Clamp(Camera.position, Vector3.Zero, new(EngineState.VoxelMap.Dimensions.X - 1, EngineState.VoxelMap.Dimensions.Y - 1, EngineState.VoxelMap.Dimensions.Z - 1));
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
                isRenderInterupted = true;
            }
            if (mouseShift.Y != 0)
            {
                Camera.rotation.X += mouseShift.Y;
                Camera.rotation.X = Math.Clamp(Camera.rotation.X, -MathF.PI / 2, MathF.PI / 2);
                isRenderInterupted = true;
            }
        }
    }
    void HandleBuilding()
    {
        if (Win.KeyboardState.IsKeyPressed(Keys.B))
            VoxelTool.PlaceSingle((Vector3i)Camera.position);

        if (!ImGui.IsAnyItemHovered() && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.None | ImGuiHoveredFlags.RootWindow))
        {
            bool placeVoxels = Win.MouseState.IsButtonDown(MouseButton.Right);
            bool removeVoxels = Win.MouseState.IsButtonDown(MouseButton.Left);

            if (placeVoxels || removeVoxels)
            {
                isRenderInterupted = true;

                Vector3 raycastDir = Camera.PixelToRay(
                    uv: Win.MouseState.Position / Win.ClientSize,
                    aspectRatio: (float)Win.ClientSize.X / Win.ClientSize.Y);

                DDAOut result = EngineState.VoxelMap.Raycast(Camera.position, raycastDir);

                if (result.hit)
                {
                    if (placeVoxels)
                        VoxelTool.PlaceSphere(
                            result.voxelHitPos + (Vector3i)Util.RotateUnitYByNormalIndex(Vector3.UnitY, result.normal), buildingRadius);
                    else
                        VoxelTool.RemoveSphere(result.voxelHitPos, buildingRadius);
                }
            }
        }
    }

    Vector3[] RayDistributionTest(int samples, float randSalt)
    {
        Vector3[] results = new Vector3[samples];
        float jitterArc = (MathF.PI * 2) / samples;

        for (int i = 1; i <= samples; i++)
        {
            Vector2 randomSampler = new Vector2(200) * i * 68.8f;
            float key1 = random(randomSampler, randSalt);
            float key2 = random(randomSampler * key1, randSalt);
            float key3 = random(new Vector2(key1 * -12.3f, key2 + 34), randSalt);
            results[i - 1] = randomVector(new Vector3(key1, key2, key3), jitterArc, i);
        }

        return results;
    }
    Vector3 randomVector(Vector3 key, float jitterArc, int sampleIndex)
    {
        float theta = jitterArc * key.X + jitterArc * sampleIndex + (MathF.PI * 2) * key.Z;
        float phi = (-1 * MathF.Sqrt(1 - key.Y * key.Y) + 1) * 1.370796f + 0.2f;
        float cosPhi = MathF.Cos(phi);
        return new Vector3(MathF.Sin(theta) * cosPhi, MathF.Sin(phi), MathF.Cos(theta) * cosPhi);
    }
    float random(Vector2 point, float randSalt)
    {
        point *= randSalt;
        float val = MathF.Sin(Vector2.Dot(point, new (12.9898f, 78.233f))) * 43758.5453123f;
        return val - (int)val;
    }
}