using ImGuiNET;

using Voxand.Content;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.UI.Components;
using Voxand.Helpers.ExtensionMethods;
using Voxand.App.Voxels.Editing;
using Voxand.Engine.Systems.Graphics.Tools.UI;
using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.Common;
using Voxand.Engine.Systems.UI.Windows;
using Voxand.Engine.Systems.Services.UI.Windows;
using Voxand.Engine.Systems.Services.Graphics;
using Voxand.Engine.Systems.Services.General;
using OpenTK.Mathematics;

using NVec2 = System.Numerics.Vector2;
using NVec4 = System.Numerics.Vector4;
using Vector2 = OpenTK.Mathematics.Vector2;
using Voxand.Engine.Systems.General.ImGuiIntegration.Systems.DragAndDrop;
using Voxand.Engine.Systems.General.ImGuiIntegration;

namespace Voxand.UI;
public static class UI_Manager
{
    static List<UI_Window> windows;

    public static void Initialize(ContentManager content)
    {
        windows = new List<UI_Window>();
    }
    
    public static void Display()
    {
        ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(mainViewport.Pos);
        ImGui.SetNextWindowSize(mainViewport.Size);
        ImGui.SetNextWindowViewport(mainViewport.ID);

        ImGui.Begin("MainDockSpace", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground);
        ImGui.DockSpace(ImGui.GetID("MainDockSpaceID"), NVec2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.End();

        foreach (UI_Window window in windows)
            window.ShowIfVisible();
    }
    public static void AddWindow(UI_Window window)
    {
        windows.Add(window);
    }
}

public abstract class UI_Window()
{
    public bool visible = true;
    public void ShowIfVisible()
    {
        if (visible) Display();
    }
    protected abstract void Display();
}
public sealed class UI_PaletteWindow : UI_Window, IVoxelMaterialSelector
{
    UI_Grid paletteGrid;
    uint[] materialDisplayColors;
    public int SelectedMaterial { get; private set; } = 0;

    readonly NVec2 CellSize = new(26, 26);
    readonly float slotBorderThickness = 2f;

    public event Action<int>? OnMaterialSelected;

    public void SetPalette(IVoxelPalette newPalette)
    {
        ArgumentNullException.ThrowIfNull(newPalette);
        BuildPaletteUI(newPalette);
        newPalette.MaterialModified += UpdatePaletteButton;
    }
    void BuildPaletteUI(IVoxelPalette palette)
    {
        materialDisplayColors = new uint[palette.MaterialCount];
        for (int i = 0; i < palette.MaterialCount; i++)
            materialDisplayColors[i] = new NVec4(Util.GetMaterialDisplayColor(palette.GetMaterial(i)).AsNum(), 1).AsImGuiU32();

        paletteGrid = new(
            label: "mygrid",
            flags: ImGuiTableFlags.None,
            itemCount: palette.MaterialCount,
            columnWidth: CellSize.X,
            gridSize: new(100, 100),
            numColumns: 0,
            gridBuilder: (i) =>
            {
                ImGui.PushID(i);

                if (ImGui.Selectable("", SelectedMaterial == i, ImGuiSelectableFlags.None, CellSize))
                    Select(i);

                ImGui.PopID();

                ImGui.GetWindowDrawList().AddRectFilled(
                    p_min: ImGui.GetItemRectMin() + new NVec2(slotBorderThickness),
                    p_max: ImGui.GetItemRectMax() - new NVec2(slotBorderThickness),
                    col: materialDisplayColors[i],
                    rounding: 0,
                    flags: ImDrawFlags.None);
            },
            onBegin: () =>
            {
                ImGui.PushStyleColor(ImGuiCol.Header, new NVec4(1, 1, 1, 1));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new NVec4(1, 1, 1, 1));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, new NVec4(1, 1, 0, 1));
            },
            onEnd: () =>
            {
                ImGui.PopStyleColor(3);
            });
    }
    protected override void Display()
    {
        ImGui.Begin("Palette");

        paletteGrid.Display();

        ImGui.End();
    }

    public void UpdatePaletteButton(int index, VoxelMaterial material)
    {
        materialDisplayColors[index] = new NVec4(Util.GetMaterialDisplayColor(material).AsNum(), 1).AsImGuiU32();
    }

    public void Select(int newActiveMaterialIndex) => OnMaterialSelected?.Invoke(newActiveMaterialIndex);

    public void ActiveMaterialChanged(int newActiveMaterialIndex) => SelectedMaterial = newActiveMaterialIndex;
}

public sealed class UI_MaterialEditorWindow : UI_Window, IVoxelMaterialEditor
{
    ColorPicker baseColorPicker;
    FloatPicker baseColorVariancePicker;
    ColorPicker emissionPicker;
    FloatPicker emissionIntensityPicker;
    VoxelMaterial material;
    public VoxelMaterial Material => material;

    public event Action<VoxelMaterial>? OnMaterialEdited;

    public UI_MaterialEditorWindow()
    {
        baseColorPicker = new("Voxel color");
        baseColorVariancePicker = new("Color variance");
        emissionPicker = new("Voxel emission");
        emissionIntensityPicker = new("Emission intensity");

        baseColorPicker.OnColorChange += (color) =>
        {
            material.baseColor = color.AsTK();
            OnMaterialEdited?.Invoke(Material);
        };

        baseColorVariancePicker.OnValueChange += (variance) =>
        {
            material.baseColorVariance = variance;
            OnMaterialEdited?.Invoke(Material);
        };

        emissionPicker.OnColorChange += (emissionColor) =>
        {
            material.emissionColor = emissionColor.AsTK();
            OnMaterialEdited?.Invoke(Material);
        };

        emissionIntensityPicker.OnValueChange += (emissionIntensity) =>
        {
            material.emissionIntensity = emissionIntensity;
            OnMaterialEdited?.Invoke(Material);
        };
    }

    protected override void Display()
    {
        ImGui.Begin("Material properties");
        baseColorPicker.Display();
        baseColorVariancePicker.Display();
        emissionPicker.Display();
        emissionIntensityPicker.Display();
        ImGui.End();
    }

    public void SetEditedMaterial(VoxelMaterial material)
    {
        this.material = material;
        baseColorPicker.color = material.baseColor.AsNum();
        baseColorVariancePicker.Value = material.baseColorVariance;
        emissionPicker.color = material.emissionColor.AsNum();
        emissionIntensityPicker.Value = material.emissionIntensity;
    }
}
public sealed class UI_SettingsWindow : UI_Window
{
    bool UseTAA = true;
    float renderResolutionPercentage = 50;
    Window win;

    IRendererAntiAliasingUsage antiAliasingUsage;
    IRendererAntiAliasing antiAliasingSettings;
    IRenderSettings renderSettings;

    public UI_SettingsWindow()
    {
        antiAliasingUsage = EngineServices.GetService<IRendererAntiAliasingUsage>();
        EngineServices.AddReplacementCallback<IRendererAntiAliasingUsage>((newAntiAliasing) =>
        {
            antiAliasingUsage = newAntiAliasing;
            UseTAA = antiAliasingUsage.UseAntiAliasing;
        });

         antiAliasingSettings = EngineServices.GetService<IRendererAntiAliasing>();
        EngineServices.AddReplacementCallback<IRendererAntiAliasing>((newSettings) => antiAliasingSettings = newSettings);

        renderSettings = EngineServices.GetService<IRenderSettings>();

        win = EngineServices.GetService<IWindowService>().Window;
        win.Resize += (args) => SetRenderingResolution(args.Size, renderResolutionPercentage / 100);
    }
    protected override void Display()
    {
        ImGui.Begin("Render settings");

        ImGui.SeparatorText("Settings");
        if (ImGui.RadioButton("Use TAA", UseTAA))
        {
            UseTAA = !UseTAA;
            antiAliasingUsage.UseAntiAliasing = UseTAA;
            antiAliasingSettings.ResetAccumulated();
        }

        ImGui.Text("Rendering resolution");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("##resSlider", ref renderResolutionPercentage, 10f, 100f, "%.0f %%"))
        {
            SetRenderingResolution(win.ClientSize, renderResolutionPercentage / 100);
        }

        ImGui.End();
    }

    void SetRenderingResolution(Vector2i baseResolution, float factor)
    {
        Vector2i newResolution = (Vector2i)((Vector2)baseResolution * factor);
        
        if (newResolution.BitwiseAnd(new Vector2i(7, 7)) != Vector2i.Zero)
            newResolution = newResolution.BitwiseAnd(new Vector2i(~7, ~7));

        renderSettings.SetRenderingResolution(newResolution);
    }
}

public sealed class UI_DebugWindow : UI_Window
{
    VoxelStructure voxelStructure;
    public UI_DebugWindow()
    {
        voxelStructure = EngineServices.GetService<IVoxelMap>().RawStructure;
        EngineServices.AddReplacementCallback<IVoxelMap>((newMap) => voxelStructure = newMap.RawStructure);
    }
    protected override void Display()
    {
        ImGui.Begin("Debug");

        ImGui.SeparatorText("Frame time");
        ImGui.Text($"FPS: {Util.FrameTimeAnalytics.FPS}");
        ImGui.Text($"Avg: {Util.FrameTimeAnalytics.AvgTime.TotalMilliseconds} ms");
        ImGui.Text($"Max: {Util.FrameTimeAnalytics.MaxTime.TotalMilliseconds} ms");
        ImGui.Text($"Min: {Util.FrameTimeAnalytics.MinTime.TotalMilliseconds} ms");

        ImGui.SeparatorText("Memory usage");
        ImGui.Text($"Voxel map RAM usage: {Math.Round(Util.BytesConverter(voxelStructure.GetMemoryUsage(), 2), 3)} mb");
        ImGui.Text($"Voxel map VRAM usage: {Math.Round(Util.BytesConverter(voxelStructure.GetGraphicsMemoryUsage(), 2), 3)} mb");

        ImGui.End();
    }
}

public sealed class UI_VoxelToolHotbar : UI_Window
{
    UI_Table hotbarTable;

    DragDropTarget[] hotbarDropTargets = new DragDropTarget[VoxelToolHotbar.MAX_SLOTS];
    DragDropSource[] hotbarDragSources = new DragDropSource[VoxelToolHotbar.MAX_SLOTS];

    float tableHeight = 0;

    public UI_VoxelToolHotbar(VoxelTool tool, VoxelToolHotbar hotbar)
    {
        for (int i = 0; i < VoxelToolHotbar.MAX_SLOTS; i++)
        {
            hotbarDropTargets[i] = new DragDropTarget(
                position: NVec2.Zero,
                size: NVec2.Zero);

            int slotIndex = i;
            hotbarDropTargets[i].OnPayloadDropped += (payload) =>
            {
                Console.WriteLine($"Dropped to {hotbarDropTargets[slotIndex].Rect.Bottom}-{hotbarDropTargets[slotIndex].Rect.Top}");

                if (payload.Type == "voxel_placement_technique_hotbar")
                {
                    (int techniqueIndex, int sourceSlotIndex) = ((int, int))payload.Data;
                    SetTechniqueIndex(hotbar, sourceSlotIndex, hotbar[slotIndex]);
                    SetTechniqueIndex(hotbar, slotIndex, techniqueIndex);
                }
                else
                {
                    SetTechniqueIndex(hotbar, slotIndex, (int)payload.Data);
                }
            };

            hotbarDragSources[i] = new DragDropSource(
                payload: new Payload(null!, "voxel_placement_technique_hotbar"),
                flags: ImGuiDragDropFlags.None,
                dragDropTooltipBuilder: (payload) =>
                {
                    ImGui.Text(tool[hotbar[slotIndex]].Name);
                })
            {
                Enabled = false
            };
        }

        hotbarTable = new UI_Table(
            label: "hotbar",
            flags: ImGuiTableFlags.None, 
            columnInfos: [new UI_TableColumnInfo("techniques", ImGuiTableColumnFlags.None, 100, 1, (row) => {
                
                int index = hotbar[row];

                NVec2 position = ImGui.GetCursorScreenPos();
                NVec2 slotSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

                ImGui.PushID(row);
                if (index < 0)
                {
                    ImGui.Selectable(
                        label: $"{(row < 9 ? row + 1 : 0)}. Empty slot",
                        selected: false,
                        flags: ImGuiSelectableFlags.None,
                        size: slotSize);
                }
                else if (ImGui.Selectable(
                    label: $"{(row < 9 ? row + 1 : 0)}. {tool[index].Name}",
                    selected: index == tool.ActivePlacementTechniqueIndex, 
                    flags: ImGuiSelectableFlags.None,
                    size: slotSize))
                {
                    tool.ActivePlacementTechniqueIndex = index;
                }
                if (ImGui.IsItemHovered())
                {
                    hotbarDropTargets[row].Enabled = true;
                    hotbarDropTargets[row].Position = position;
                    hotbarDropTargets[row].Size = slotSize;

                    hotbarDragSources[row].Enabled = true;
                    hotbarDragSources[row].Position = position;
                    hotbarDragSources[row].Size = slotSize;
                }
                else 
                {
                    hotbarDropTargets[row].Enabled = false;
                    hotbarDragSources[row].Enabled = false;
                }
                ImGui.PopID();
            })], 
            numRows: 10,
            displayHeadersRow: false);
    }

    void SetTechniqueIndex(VoxelToolHotbar hotbar, int hotbarItemIndex, int techniqueIndex)
    {
        if (hotbarItemIndex < 0 || hotbarItemIndex >= VoxelToolHotbar.MAX_SLOTS)
            throw new ArgumentOutOfRangeException(nameof(hotbarItemIndex), "Hotbar item index must be between zero and " + (VoxelToolHotbar.MAX_SLOTS - 1));

        hotbar[hotbarItemIndex] = techniqueIndex;
        hotbarDragSources[hotbarItemIndex].Payload.Data = (techniqueIndex, hotbarItemIndex);
        hotbarDragSources[hotbarItemIndex].Enabled = techniqueIndex < 0 ? false : true;
    }

    protected override void Display()
    {
        if (tableHeight == 0)
        {
            ImGui.Begin("_tmp");
            hotbarTable.Display();
            tableHeight = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
            ImGui.End();
        }
        ImGui.SetNextWindowSizeConstraints(new(0, 0), new(float.MaxValue, tableHeight));
        ImGui.Begin("Voxel Tool Hotbar");
        hotbarTable.Display();
        ImGui.End();
    }
}

public sealed class UI_VoxelToolSettingsWindow : UI_Window
{
    VoxelTool tool;
    VoxelToolController toolController;
    InspectorMenu toolConfigMenu;
    (string name, DragDropSource dragDropSource)[] placementTechniqueDescriptors;
    DragDropSource previewDragDropSource;
    public UI_VoxelToolSettingsWindow(VoxelTool tool, VoxelToolController toolController)
    {
        this.tool = tool;
        this.toolController = toolController;

        BuildPlacementTechniqueDescriptors();
        tool.OnPlacementTechniquesLoaded += BuildPlacementTechniqueDescriptors;

        toolConfigMenu = new(tool.ActivePlacementTechnique);

        tool.OnPlacementTechniqueChanged += (index) =>
        {
            try
            {
                toolConfigMenu = new(tool[index]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            previewDragDropSource!.Payload.Data = index;
            string activeTechniqueName = tool.ActivePlacementTechnique.Name;
            previewDragDropSource.DragDropTooltipBuilder = (payload) => ImGui.Text(activeTechniqueName);
        };
    }

    protected override void Display()
    {
        ImGui.Begin("Voxel tool");

        ImGui.SeparatorText("Modes");

        bool comboOpen = ImGui.BeginCombo("", tool.ActivePlacementTechnique.Name);

        previewDragDropSource.CoverLastImGuiItem();

        if (comboOpen)
        {
            for (int i = 0; i < placementTechniqueDescriptors.Length; i++)
            {
                if (ImGui.Selectable(placementTechniqueDescriptors[i].name, i == tool.ActivePlacementTechniqueIndex))
                    tool.ActivePlacementTechniqueIndex = i;

                placementTechniqueDescriptors[i].dragDropSource.CoverLastImGuiItem();
            }
            ImGui.EndCombo();
        }

        if (tool.ActivePlacementTechnique is ComplexPlacementTechnique)
        {
            ImGui.TextColored(new NVec4(1, 0.8078f, 0.2784f, 1), "Enter - apply\nBackspace - cancel");
        }

        ImGui.SeparatorText("Settings");

        if (tool.ActivePlacementTechnique is not ComplexPlacementTechnique)
        {
            bool automatic = toolController.AutomaticMode;
            ImGui.Checkbox("Automatic", ref automatic);
            toolController.AutomaticMode = automatic;

            float delay = toolController.AutomaticModeDelay * 1000;
            ImGui.SliderFloat("delay", ref delay, 1f, 400f, "%.1f ms");
            toolController.AutomaticModeDelay = delay / 1000;
        }

        toolConfigMenu.Display();

        ImGui.End();
    }

    void BuildPlacementTechniqueDescriptors()
    {
        placementTechniqueDescriptors = new (string, DragDropSource)[tool.TechniqueCount];
        for (int i = 0; i < tool.TechniqueCount; i++)
        {
            string techniqueName = tool[i].Name;
            DragDropSource dragDropSource = new(
                payload: new Payload(i, "voxel_placement_technique"),
                flags: ImGuiDragDropFlags.None,
                dragDropTooltipBuilder: (payload) =>
                {
                    ImGui.Text(techniqueName);
                });
            placementTechniqueDescriptors[i] = (techniqueName, dragDropSource);
        }

        int activeTechniqueIndex = tool.ActivePlacementTechniqueIndex;
        string activeTechniqueName = tool.ActivePlacementTechnique.Name;
        previewDragDropSource = new DragDropSource(
            payload: new Payload(tool.ActivePlacementTechniqueIndex, "voxel_placement_technique"),
            flags: ImGuiDragDropFlags.None,
            dragDropTooltipBuilder: (payload) =>
            {
                ImGui.Text(activeTechniqueName);
            });
    }
}
