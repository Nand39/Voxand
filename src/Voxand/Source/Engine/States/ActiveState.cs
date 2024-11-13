using System.Diagnostics;

using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using GLAV.Types;

using Voxand.Engine;
using Voxand.Engine.GameStates;
using Voxand.Engine.Graphics;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.Helpers.Utility;
using Voxand.UI;

namespace Voxand;
public class ActiveState(Voxand game) : GameState(game)
{
    bool cameraFollow;
    float sensitivity = 0.0038f;
    float speed = 13;
    bool fastMovement = false;
    bool hideCursor = false;


    Vector3i mapSize = new(256, 256, 256);

    VoxelBrickmap voxelMap;
    VoxelPalette voxelPalette;
    VoxandRendererActive renderer;
    UI_Manager ui;
    
    Framewatch fps;

    Vector2i screenCenter;

    public static uint material = 0;

    int renderTechniqueIndex = 1;

    bool lockMovement = false;

    Texture2D texture;
    public override void Load()
    {
        Console.WriteLine($"{(int)TextureUnit.Texture0}, {(int)TextureUnit.Texture1}");

        GL.ClearColor(0.3f, 0.3f, 0.2f, 1);

        Camera.position = (Vector3)mapSize / 2 + new Vector3(-3, 3, 3);
        Camera.screenSize = new Vector2i(128, 128);
        Camera.FOV = 90;

        if (hideCursor) 
            unsafe { GLFW.SetInputMode(main.WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorHidden); }

        voxelPalette = new VoxelPalette();

        voxelMap = new(mapSize, new VoxelBrickmapDefaultPersistenceModule());

        renderer = new VoxandRendererActive(main.content);
        ui = new UI_Manager(voxelPalette, main.content);

        renderer.SetMapSize(mapSize);
        renderer.SetTechnique((RenderTechniques)renderTechniqueIndex);

        fps = new Framewatch(Util.FrameTimeData, 1);

        ui.OnRenderTechniqueChangeRequest += (args) =>
        {
            renderTechniqueIndex++;
            renderTechniqueIndex %= Enum.GetNames(typeof(RenderTechniques)).Length;
            renderer.SetTechnique((RenderTechniques)renderTechniqueIndex);
        };

        Util.CurrentMap = voxelMap;

        voxelMap.SetVoxelValueAndBit(voxelMap.Dimensions / 2, 0, true);

        Console.WriteLine("Loaded");
    }
    public override void Update(FrameEventArgs args)
    {

        #region Camera-mouse follow
        if (main.KeyboardState.IsKeyPressed(Keys.E))
        {
            if (cameraFollow)
            {
                cameraFollow = false;
                if (hideCursor)
                    unsafe { GLFW.SetInputMode(main.WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorNormal); }
            }
            else
            {
                cameraFollow = true;
                main.MousePosition = screenCenter;
                if (hideCursor)
                    unsafe { GLFW.SetInputMode(main.WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorHidden); }
            }
        }
        else if (cameraFollow)
        {
            Vector2 mouseShift = (main.MouseState.Position - screenCenter) * sensitivity;
            main.MousePosition = screenCenter;

            Camera.rotation.Y += mouseShift.X; Camera.rotation.Y %= MathF.Tau;
            Camera.rotation.X += mouseShift.Y; 
            Camera.rotation.X = Math.Clamp(Camera.rotation.X, -1.57079f, 1.57079f);
        }
        #endregion

        #region Building
        if (main.KeyboardState.IsKeyPressed(Keys.Z))
        {
            material += 1;
            material %= (uint)voxelPalette.PaletteLength;
        }
        else if (main.KeyboardState.IsKeyPressed(Keys.X))
        {
            material -= 1;
            material %= (uint)voxelPalette.PaletteLength;
        }

        if (!ImGui.IsAnyItemHovered() && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.None | ImGuiHoveredFlags.RootWindow))
        {
            if (main.MouseState.IsButtonPressed(MouseButton.Right))
            {
                Stopwatch sw = Stopwatch.StartNew();

                DDAOut result = voxelMap.Raycast(Camera.position, Camera.PixelToRay((Vector2i)main.MouseState.Position));

                sw.Stop();
                Console.WriteLine($"Raycast time = {sw.Elapsed.TotalMilliseconds} ms");

                if (result.hit)
                {
                    Vector3i p = default;
                    p.Y = result.voxelHitPos.Y;
                    for (p.X = result.voxelHitPos.X - 10; p.X <= result.voxelHitPos.X + 10; p.X ++)
                    {
                        for (p.Z = result.voxelHitPos.Z - 10; p.Z <= result.voxelHitPos.Z + 10; p.Z++)
                        {
                            voxelMap.SetVoxelValueAndBit(p, material, true);
                        }
                    }
                    
                }
            }
            if (main.MouseState.IsButtonPressed(MouseButton.Left))
            {
                Stopwatch sw = Stopwatch.StartNew();

                DDAOut result = voxelMap.Raycast(Camera.position, Camera.PixelToRay((Vector2i)main.MouseState.Position));

                sw.Stop();
                Console.WriteLine($"Raycast time = {sw.Elapsed.TotalMilliseconds} ms");

                if (result.hit)
                {
                    voxelMap.SetVoxelValueAndBit(result.voxelHitPos + (Vector3i)Util.RotateVerticalByNormalIndex(Vector3.UnitY, result.normal), material, true);
                }
            }
        }


        if (main.KeyboardState.IsKeyPressed(Keys.T)) renderer.agressiveTAA = !renderer.agressiveTAA;

        #endregion

        #region Map save/load
        if (main.KeyboardState.IsKeyPressed(Keys.O))
        {
            //voxelMap.SaveMap();
        }
        if (main.KeyboardState.IsKeyPressed(Keys.I))
        {
            //voxelMap.LoadMap();
        }
        #endregion

        #region Controls

        if (!lockMovement)
        {
            float speed = this.speed * (float)args.Time;
            Vector3 forward, tangent; Vector3 movement = Vector3.Zero;

            forward = Util.RotateY(Vector3.UnitZ, Camera.rotation.Y);
            tangent = Vector3.Cross(Vector3.UnitY, forward);

            if (main.KeyboardState.IsKeyDown(Keys.W))
            {
                movement.Z--;
            }
            if (main.KeyboardState.IsKeyDown(Keys.S))
            {
                movement.Z++;
            }
            if (main.KeyboardState.IsKeyDown(Keys.A))
            {
                movement.X--;
            }
            if (main.KeyboardState.IsKeyDown(Keys.D))
            {
                movement.X++;
            }
            if (main.KeyboardState.IsKeyDown(Keys.Space))
            {
                movement.Y++;
            }
            if (main.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                movement.Y--;
            }
            if (main.KeyboardState.IsKeyPressed(Keys.LeftControl)) fastMovement = !fastMovement;

            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                if (fastMovement) movement *= 8;
                Camera.position += forward * movement.Z * speed;
                Camera.position += tangent * movement.X * speed;
                Camera.position += Vector3.UnitY * movement.Y * speed;
            }

            Camera.position = Vector3.Clamp(Camera.position, Vector3.Zero, new(mapSize.X - 1, mapSize.Y - 1, mapSize.Z - 1));
        }
        #endregion

        if (main.KeyboardState.IsKeyPressed(Keys.L)) lockMovement = lockMovement ? false : true;

        if (main.KeyboardState.IsKeyPressed(Keys.B))
            voxelMap.SetVoxelValueAndBit((Vector3i)Camera.position, material, true);


        if (main.KeyboardState.IsKeyPressed(Keys.R))
        {
            (uint value, uint bit, int brickIndex) = voxelMap.Examine((Vector3i)Camera.position, false);
            Console.WriteLine($"! CPU SIDE: voxel data at {(Vector3i)Camera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
            (value, bit, brickIndex) = voxelMap.Examine((Vector3i)Camera.position, true);
            Console.WriteLine($"* GPU SIDE: voxel data at {(Vector3i)Camera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
        }

        fps.Tick(args);
    }
    public override void Render(FrameEventArgs args)
    {
        renderer.RenderVoxels((float)args.Time);

        renderer.Composite();

        renderer.Postprocess(renderer.settings.CompositingResultTexture);

        ui.Display();
    }
    public override void Unload()
    {
        renderer.Dispose();
        voxelMap.Dispose();

        Console.WriteLine("Resources was successfully unloaded");
    }

    public override void OnResize(ResizeEventArgs args)
    {
        screenCenter = args.Size / 2;
        Camera.screenSize = args.Size;
        Camera.RefreshProjection();
        renderer.OnResize(args);
    }
}