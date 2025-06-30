using System.Runtime.CompilerServices;
using System.Text.Json;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using GLAV.Types.Extended;
using GLAV.Types;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods;

using NVec2 = System.Numerics.Vector2;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Content;
using Voxand.Engine.Systems.Structures.PrimitiveStructs;
using DisposableExt;
using Voxand.Engine.Systems.General.ImGuiIntegration.Systems.DragAndDrop;

namespace Voxand.Engine.Systems.General.ImGuiIntegration;
public class ImGuiBackend : IDisposableExt
{
    bool _frameBegun;

    NVec2 scaleFactor = NVec2.One;

    NativeWindow window;

    public DisposeHelper DisposeHelper { get; }

    Keys[] openTK_keys = (Keys[])Enum.GetValues(typeof(Keys));

    IImGuiInputCacheInternals imGuiInputCache;
    private interface IImGuiInputCacheInternals
    {
        void Update();
    }
    public class ImGuiInputCache : IImGuiInputCacheInternals
    {
        public class FrameInputState
        {
            public NVec2 MousePosition { get; set; }
            public MouseButtonsMask IsMouseButtonDragging { get; set; }
        }

        public FrameInputState State { get; private set; }
        public FrameInputState Past { get; private set; }

        public NVec2[] mouseClickPositions = new NVec2[3];

        public ImGuiInputCache()
        {
            State = new FrameInputState();
            Past = new FrameInputState();
        }

        void IImGuiInputCacheInternals.Update()
        {
            MakeCurrentStatePrevious();
            UpdateInput();
        }

        void MakeCurrentStatePrevious()
        {
            FrameInputState temp = Past;
            Past = State;
            State = temp;
        }
        void UpdateInput()
        {
            State.MousePosition = ImGui.GetMousePos();

            State.IsMouseButtonDragging = 0;
            for (int i = 0; i < 3; i++)
            {
                if (ImGui.IsMouseDragging((ImGuiMouseButton)i))
                    State.IsMouseButtonDragging |= (MouseButtonsMask)(1 << i);

                if (ImGui.IsMouseClicked((ImGuiMouseButton)i))
                    mouseClickPositions[i] = State.MousePosition;
            }
        }
    }

    #region Drag-and-drop

    static Payload? activePayload;
    static Action<Payload>? dragDropTooltipBuilder;
    public static bool IsDraggingPayload { get; private set; }
    
    static void BeginDragDrop(Payload payload, Action<Payload> dragDropTooltipBuilder)
    {
        activePayload = payload;
        IsDraggingPayload = true;
        ImGuiBackend.dragDropTooltipBuilder = dragDropTooltipBuilder;
    }
    static void EndDragDrop()
    {
        if (!IsDraggingPayload)
            throw new InvalidOperationException("Cannot end drag-drop when no payload is being dragged.");

        IsDraggingPayload = false;

        NVec2 mousePos = ImGui.GetMousePos();

        foreach (DragDropTarget target in dragDropTargets)
        {
            if (!target.Enabled)
                continue;

            if (ImGuiInput.IsHovering(target.Rect))
                target.HandlePayloadDropped(activePayload!);
        }

        activePayload = null;
        dragDropTooltipBuilder = null;
    }

    static HashSet<DragDropSource> dragDropSources = new();
    static void OnDragStarted(MouseButtonsMask mouseButtons)
    {
        foreach (DragDropSource source in dragDropSources)
        {
            if (!source.Enabled)
                continue;

            if ((source.TrackedMouseButtons & mouseButtons) != 0)
            {
                if (!ImGuiInput.WasHoveringOnLastClick(source.Rect, mouseButtons))
                    continue;

                BeginDragDrop(source.Payload, source.DragDropTooltipBuilder);
            }
                
        }
    }


    static HashSet<DragDropTarget> dragDropTargets = new();
    public static void RegisterDragDropTarget(DragDropTarget target)
    {
        dragDropTargets.Add(target);
    }
    public static void RegisterDragDropSource(DragDropSource target)
    {
        dragDropSources.Add(target);
    }

    static void RenderDragDropTooltip()
    {
        if (IsDraggingPayload)
        {
            ImGui.BeginTooltip();
            dragDropTooltipBuilder?.Invoke(activePayload!);
            ImGui.EndTooltip();
        }
    }

    #endregion


    public ImGuiBackend(NativeWindow win, ContentManager content)
    {
        DisposeHelper = new(this);

        window = win;

        nint context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.MouseDragThreshold = 3f;

        CreateImGuiResources(content);

        ImGuiInputCache imGuiInputCacheObject = new ImGuiInputCache();
        imGuiInputCache = imGuiInputCacheObject;
        ImGuiInput.Initialize(imGuiInputCacheObject);
    }

    VertexSpecification vertexSpecification;
    TypedArray<ImGuiVertex> vertexArray;
    TypedArray<ushort> elementArray;
    ShaderController shaderController;
    public void CreateImGuiResources(ContentManager content)
    {
        vertexArray = new TypedArray<ImGuiVertex>(BufferTarget.ArrayBuffer, 500, BufferUsageHint.DynamicDraw);
        vertexArray.Label = "ImGui Vertex Attribute Array";

        elementArray = new TypedArray<ushort>(BufferTarget.ElementArrayBuffer, 500, BufferUsageHint.DynamicDraw);
        elementArray.Label = "ImGui Element Array";

        vertexSpecification = new();
        vertexSpecification.AddAttributeSource(vertexArray);
        vertexSpecification.Label = "ImGui Vertex Specification";

        shaderController = new ShaderController(content, 
            "Graphics/Shaders/ImGui/imgui_vertex_shader.vert", 
            "Graphics/Shaders/ImGui/imgui_fragment_shader.frag");
        shaderController.Shader.Label = "ImGui Shader Program";
        
        CreateFontTexture();
    }

    Texture2D fontTexture;

    public void CreateFontTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out nint dataPtr, out int width, out int height, out int bytesPerPixel);

        int mipmapLevelCount = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        Vector2i atlasSize = new(width, height);
        fontTexture = new(atlasSize, SizedInternalFormat.Rgba8, mipmapLevelCount);
        fontTexture.Label = "ImGui Text Atlas";
        fontTexture.Store(0, Vector2i.Zero, atlasSize, new(PixelFormat.Bgra, PixelType.UnsignedByte), dataPtr);
        fontTexture.GenMipmaps();

        fontTexture.SetParams([
            new TexParam(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat),
            new TexParam(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat),
            new TexParam(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear),
            new TexParam(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear),
            new TexParam(TextureParameterName.TextureMaxLevel, mipmapLevelCount - 1),
            ]);

        ImportImGuiTexture(fontTexture);

        io.Fonts.SetTexID(fontTexture.Handle.id);
        io.Fonts.ClearTexData();
    }

    public void Render()
    {
        if (IsDraggingPayload)
        {
            if (ImGuiInput.IsAnyMouseButtonDragChanged())
                EndDragDrop();
        }
        else if (ImGuiInput.MouseDragStarted())
        {
            OnDragStarted(ImGuiInput.GetMouseDragMask());
        }

        RenderDragDropTooltip();

        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    public void Update()
    {
        if (_frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData();
        UpdateImGuiInput();

        _frameBegun = true;
        ImGui.NewFrame();

        imGuiInputCache.Update();
    }

    private void SetPerFrameImGuiData()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DeltaTime = FrameTime.Delta;
        io.DisplaySize = new NVec2(
            window.ClientSize.X / scaleFactor.X,
            window.ClientSize.Y / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
    }

    readonly List<uint> PressedChars = [];

    private void UpdateImGuiInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        MouseState MouseState = window.MouseState;
        KeyboardState KeyboardState = window.KeyboardState;

        io.MousePos = MouseState.Position.AsNum();

        io.MouseDown[0] = MouseState[MouseButton.Left];
        io.MouseDown[1] = MouseState[MouseButton.Right];
        io.MouseDown[2] = MouseState[MouseButton.Middle];

        io.KeyCtrl = KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = KeyboardState.IsKeyDown(Keys.LeftSuper) || KeyboardState.IsKeyDown(Keys.RightSuper);

        foreach (Keys key in openTK_keys)
        {
            if (key != Keys.Unknown)
                io.AddKeyEvent(TranslateKey(key), KeyboardState.IsKeyDown(key));
        }

        foreach (var c in PressedChars)
        {
            io.AddInputCharacter(c);
        }
        PressedChars.Clear();
    }

    public void PressChar(uint codepoint) => PressedChars.Add(codepoint);

    internal void MouseScroll(Vector2 offset)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.MouseWheel = offset.Y;
        io.MouseWheelH = offset.X;
    }



    public delegate void UserDrawCommand(ImDrawListPtr drawListPtr, ImDrawCmdPtr drawCommandPtr);

    Queue<UserDrawCommand> userDrawCommands = [];

    public void InsertUserDrawCommand(UserDrawCommand command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        userDrawCommands.Enqueue(command);
    }

    Dictionary<int, Texture2D> imGuiTextures = [];
    public void ImportImGuiTexture(Texture2D texture)
    {
        imGuiTextures[texture.Handle.id] = texture;
    }

    int numVertB = 0;

    unsafe void RenderImDrawData(ImDrawDataPtr draw_data)
    {
        if (draw_data.CmdListsCount == 0)
            return;

        bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        PolygonMode prevPolygonMode = (PolygonMode)GL.GetInteger(GetPName.PolygonMode);

        Rect prevScissorBox = default;
        GL.GetInteger(GetPName.ScissorBox, (int*)&prevScissorBox);

        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        int requiredVertexCapacity = 0;
        int requiredElementCapacity = 0;
        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[i];

            requiredVertexCapacity = Math.Max(cmd_list.VtxBuffer.Size, requiredVertexCapacity);
            requiredElementCapacity = Math.Max(cmd_list.IdxBuffer.Size, requiredElementCapacity);
            
        }

        if (requiredVertexCapacity > vertexArray.Length)
        {
            int newCapacity = Math.Max((int)(vertexArray.Length * 1.5f), requiredVertexCapacity);

            vertexArray.Dispose();
            numVertB++;
            vertexArray = new TypedArray<ImGuiVertex>(BufferTarget.ArrayBuffer, newCapacity, BufferUsageHint.DynamicDraw);
            vertexArray.Label = $"ImGui Vertex Attribute Array #{numVertB}";

            vertexSpecification.Clear();
            vertexSpecification.AddAttributeSource(vertexArray);

            Console.WriteLine($"Increased ImGui vertex array capacity to {newCapacity}");
        }
        if (requiredElementCapacity > elementArray.Length)
        {
            int newCapacity = Math.Max((int)(elementArray.Length * 1.5f), requiredElementCapacity);

            elementArray.Dispose();
            elementArray = new TypedArray<ushort>(BufferTarget.ElementArrayBuffer, newCapacity, BufferUsageHint.DynamicDraw);
            elementArray.Label = "ImGui Element Array";

            Console.WriteLine($"Increased ImGui element array capacity to {newCapacity}");
        }

        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4 imGuiTransform = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        shaderController.SetUniform("projection_matrix", imGuiTransform);
        shaderController.SetUniform("in_fontTexture", 0);

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        for (int drawListIndex = 0; drawListIndex < draw_data.CmdListsCount; drawListIndex++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[drawListIndex];

            Span<ImGuiVertex> vertices = new((void*)cmd_list.VtxBuffer.Data, cmd_list.VtxBuffer.Size);
            Span<ushort> indices = new((void*)cmd_list.IdxBuffer.Data, cmd_list.IdxBuffer.Size);

            vertexArray.Store(vertices, 0);
            elementArray.Store(indices, 0);
            vertexSpecification.SetElementBuffer(elementArray);

            for (int commandIndex = 0; commandIndex < cmd_list.CmdBuffer.Size; commandIndex++)
            {
                ImDrawCmdPtr drawCommandPtr = cmd_list.CmdBuffer[commandIndex];
                if (drawCommandPtr.UserCallback != nint.Zero)
                {
                    userDrawCommands.Dequeue()(cmd_list, drawCommandPtr);
                }
                else
                {
                    imGuiTextures[(int)drawCommandPtr.TextureId].BindTex(0);

                    RectF clip = *(RectF*)Unsafe.AsPointer(ref drawCommandPtr.ClipRect);
                    GL.Scissor((int)clip.Left, window.ClientSize.Y - (int)clip.Height, (int)(clip.Width - clip.Left), (int)(clip.Height - clip.Bottom));

                    vertexSpecification.DrawElements(PrimitiveType.Triangles, (int)drawCommandPtr.ElemCount, DrawElementsType.UnsignedShort, (int)drawCommandPtr.IdxOffset * sizeof(ushort), unchecked((int)drawCommandPtr.VtxOffset));
                }
            }
        }
        GL.Scissor(prevScissorBox.position.X, prevScissorBox.position.Y, prevScissorBox.size.X, prevScissorBox.size.Y);
        GL.PolygonMode(MaterialFace.FrontAndBack, prevPolygonMode);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)prevBlendFuncSrcRgb,
            (BlendingFactorDest)prevBlendFuncDstRgb,
            (BlendingFactorSrc)prevBlendFuncSrcAlpha,
            (BlendingFactorDest)prevBlendFuncDstAlpha);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
    }

    public static ImGuiKey TranslateKey(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F24)
            return key - Keys.F1 + ImGuiKey.F24;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            case Keys.LeftShift: return ImGuiKey.LeftShift;
            case Keys.LeftControl: return ImGuiKey.LeftCtrl;
            case Keys.LeftAlt: return ImGuiKey.LeftAlt;
            case Keys.LeftSuper: return ImGuiKey.LeftSuper;
            case Keys.RightShift: return ImGuiKey.RightShift;
            case Keys.RightControl: return ImGuiKey.RightCtrl;
            case Keys.RightAlt: return ImGuiKey.RightAlt;
            case Keys.RightSuper: return ImGuiKey.RightSuper;
            case Keys.Menu: return ImGuiKey.Menu;
            default: return ImGuiKey.None;
        }
    }

    void IDisposableExt.Free()
    {
        vertexArray.Dispose();
        elementArray.Dispose();
        vertexSpecification.Dispose();
        shaderController.Dispose();
        fontTexture.Dispose();
    }
}