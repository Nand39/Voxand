using System.Diagnostics;

using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using DisposableExt;

using Voxand.Engine.GameStates;
using Voxand.Engine.Graphics;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.Helpers.UtilityObjects;
using Voxand.UI;
using Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP;
using Voxand.Engine.Graphics.Pipelines.Modules;

namespace Voxand;
public class ActiveState(Window game) : GameState(game)
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
    Camera mainCamera;

    VoxelPTRP renderingPipeline;
    
    Framewatch fps;

    Vector2i screenCenter;

    public static uint material = 0;

    int renderTechniqueIndex = 1;

    bool lockMovement = false;

    public override void Load()
    {
        Console.WriteLine($"{(int)TextureUnit.Texture0}, {(int)TextureUnit.Texture1}");

        GL.ClearColor(0.6f, 0.3f, 0.2f, 1);

        mainCamera = new Camera();
        mainCamera.position = (Vector3)mapSize / 2 + new Vector3(-3, 3, 3);
        mainCamera.FOV = 90;

        if (hideCursor) 
            unsafe { GLFW.SetInputMode(main.WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorHidden); }

        voxelPalette = new VoxelPalette();

        voxelMap = new(mapSize, new VoxelBrickmapDefaultPersistenceModule());

        renderer = new VoxandRendererActive(main.content) { camera = mainCamera };
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

        renderingPipeline = new(main.content, mainCamera, voxelMap, main.ClientSize);

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

            mainCamera.rotation.Y += mouseShift.X; mainCamera.rotation.Y %= MathF.Tau;
            mainCamera.rotation.X += mouseShift.Y;
            mainCamera.rotation.X = Math.Clamp(mainCamera.rotation.X, -1.57079f, 1.57079f);
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

                DDAOut result = voxelMap.Raycast(mainCamera.position, mainCamera.PixelToRay(main.MouseState.Position / main.ClientSize, (float)main.ClientSize.X / main.ClientSize.Y));

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

                DDAOut result = voxelMap.Raycast(mainCamera.position, mainCamera.PixelToRay(main.MouseState.Position / main.ClientSize, (float)main.ClientSize.X / main.ClientSize.Y));

                sw.Stop();
                Console.WriteLine($"Raycast time = {sw.Elapsed.TotalMilliseconds} ms");

                if (result.hit)
                {
                    voxelMap.SetVoxelValueAndBit(result.voxelHitPos + (Vector3i)Util.RotateVerticalByNormalIndex(Vector3.UnitY, result.normal), material, true);
                }
            }
        }

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

            forward = Util.RotateY(Vector3.UnitZ, mainCamera.rotation.Y);
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
                mainCamera.position += forward * movement.Z * speed;
                mainCamera.position += tangent * movement.X * speed;
                mainCamera.position += Vector3.UnitY * movement.Y * speed;
            }

            mainCamera.position = Vector3.Clamp(mainCamera.position, Vector3.Zero, new(mapSize.X - 1, mapSize.Y - 1, mapSize.Z - 1));
        }
        #endregion

        if (main.KeyboardState.IsKeyPressed(Keys.L)) lockMovement = lockMovement ? false : true;

        if (main.KeyboardState.IsKeyPressed(Keys.B))
            voxelMap.SetVoxelValueAndBit((Vector3i)mainCamera.position, material, true);

        if (main.KeyboardState.IsKeyPressed(Keys.T))
        {
            Console.WriteLine(renderer.agressiveTAA ? "ATAA enabled" : "ATAA disabled");
            renderer.agressiveTAA = !renderer.agressiveTAA;
        }

        if (main.KeyboardState.IsKeyPressed(Keys.R))
        {
            (uint value, uint bit, int brickIndex) = voxelMap.Examine((Vector3i)mainCamera.position, false);
            Console.WriteLine($"! CPU SIDE: voxel data at {(Vector3i)mainCamera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
            (value, bit, brickIndex) = voxelMap.Examine((Vector3i)mainCamera.position, true);
            Console.WriteLine($"* GPU SIDE: voxel data at {(Vector3i)mainCamera.position}: value={value}; empty={bit == 0}; brick={brickIndex}");
        }

        //Console.WriteLine("scroll: " + main.MouseState.Scroll.Y * 0.2f);
        renderer.compositeShaderInfo.SetUniform("wp", main.MouseState.Scroll.Y * 0.2f);

        fps.Tick(args);
    }
    public override void Render(FrameEventArgs args)
    {
        //renderingPipeline.MapSizeChanged(mapSize);

        //renderingPipeline.Execute();

        //renderer.RenderVoxelsCompute();

        renderingPipeline.VPT();
        renderingPipeline.TAA();
        renderingPipeline.CMP();
        //renderingPipeline.IPP();

        //renderer.Composite();
        //renderer.Postprocess(renderer.settings.CompositingResultTexture);

        ui.Display();
    }
    public override void Unload()
    {
        renderer.Dispose();
        renderingPipeline.Dispose();
        voxelMap.Dispose();

        Console.WriteLine("Resources was successfully unloaded");
    }

    public override void OnResize(ResizeEventArgs args)
    {
        screenCenter = args.Size / 2;
        renderer.OnResize(args);
        renderingPipeline.SetFinalResolution(args.Size);
    }
}