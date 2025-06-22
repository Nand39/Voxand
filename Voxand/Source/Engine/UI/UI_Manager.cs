using System.Numerics;

using ImGuiNET;

using Voxand.Content;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.UI.Components;
using Voxand.Engine.Systems.General.Events;
using Voxand.Helpers.ExtensionMethods;
using Voxand.App.Map;
using Voxand.App.VoxelEditing;
using Voxand.Helpers.Reflection;
using System.Reflection;
using Voxand.Engine.Systems.Graphics.Tools.UI;
using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Voxand.UI;
public static class UI_Manager
{
    static List<UI_Window> windows;

    public static EventDispatcher Events = new("ui_events");
    public static void Initialize(ContentManager content)
    {
        windows = new List<UI_Window>();

        try
        {
            string styleJSON = content.ReadFile("Graphics/UI/ImGuiStyles/style.json");
            ImGuiController.SetStyle(styleJSON);
        }
        catch { }
    }
    
    public static void Display()
    {
        ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(mainViewport.Pos);
        ImGui.SetNextWindowSize(mainViewport.Size);
        ImGui.SetNextWindowViewport(mainViewport.ID);

        ImGui.Begin("MainDockSpace", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground);
        ImGui.DockSpace(ImGui.GetID("MainDockSpaceID"), Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.End();

        foreach (UI_Window window in windows)
            window.ShowIfVisible();
    }
    public static void AddWindow(UI_Window window)
    {
        Events.ImportEvents(window);
        Events.ExportEvents(window);
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
public sealed class UI_PaletteWindow : UI_Window
{
    UI_Grid paletteGrid;
    uint[] materialDisplayColors;
    public int SelectedMaterial { get; private set; } = 0;

    readonly Vector2 CellSize = new(26, 26);
    readonly float slotBorderThickness = 2f;

    [ExportEvent("ui_events", "voxel_material_selected")] 
    public event Action<object>? OnMaterialSelected;

    [ImportEvent("engine_state_events", "main_voxelPalette_changed")]
    public void SetPalette(object newPalette)
    {
        ArgumentNullException.ThrowIfNull(newPalette);
        BuildPaletteUI((VoxelPalette)newPalette);
    }
    void BuildPaletteUI(VoxelPalette palette)
    {
        materialDisplayColors = new uint[palette.MaterialCount];
        for (int i = 0; i < palette.MaterialCount; i++)
            materialDisplayColors[i] = new Vector4(Util.GetMaterialDisplayColor(palette.GetMaterial(i)).AsNum(), 1).AsImGuiU32();

        paletteGrid = new(
            label: "mygrid",
            flags: ImGuiTableFlags.None,
            itemCount: palette.MaterialCount,
            sizeMode: UI_Grid.GridSizeMode.FixedColumnWidth,
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
                    p_min: ImGui.GetItemRectMin() + new Vector2(slotBorderThickness),
                    p_max: ImGui.GetItemRectMax() - new Vector2(slotBorderThickness),
                    col: materialDisplayColors[i],
                    rounding: 0,
                    flags: ImDrawFlags.None);
            },
            onBegin: () =>
            {
                ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(1, 1, 1, 1));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1, 1, 1, 1));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1, 1, 0, 1));
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

    [ImportEvent("voxelPalette_events", "material_modified")]
    public void UpdatePaletteButton((int index, VoxelMaterial material) arg)
    {
        materialDisplayColors[arg.index] = new Vector4(Util.GetMaterialDisplayColor(arg.material).AsNum(), 1).AsImGuiU32();
    }

    public void Select(int newActiveMaterialIndex) => OnMaterialSelected?.Invoke(newActiveMaterialIndex);

    [ImportEvent("user_voxelTool_events", "activeMaterial_changed")]
    public void ActiveMaterialChanged(int newActiveMaterialIndex) => SelectedMaterial = newActiveMaterialIndex;
}

public sealed class UI_MaterialEditorWindow : UI_Window
{
    ColorPicker baseColorPicker;
    FloatPicker baseColorVariancePicker;
    ColorPicker emissionPicker;
    FloatPicker emissionIntensityPicker;
    VoxelPalette palette;
    int selectedMaterial;

    [ExportEvent("ui_events", "voxel_material_edited")] 
    public event Action<object>? OnMaterialEdited;

    public UI_MaterialEditorWindow(VoxelPalette palette)
    {
        this.palette = palette;

        baseColorPicker = new("Voxel color");
        baseColorVariancePicker = new("Color variance");
        emissionPicker = new("Voxel emission");
        emissionIntensityPicker = new("Emission intensity");

        baseColorPicker.OnColorChange += (color) =>
        {
            OnMaterialEdited?.Invoke((palette.GetMaterial(selectedMaterial) with { baseColor = color.AsTK() }, selectedMaterial));
        };

        baseColorVariancePicker.OnValueChange += (variance) =>
        {
            OnMaterialEdited?.Invoke((palette.GetMaterial(selectedMaterial) with { baseColorVariance = variance}, selectedMaterial));
        };

        emissionPicker.OnColorChange += (emissionColor) =>
        {
            OnMaterialEdited?.Invoke((palette.GetMaterial(selectedMaterial) with { emissionColor = emissionColor.AsTK() }, selectedMaterial));
        };

        emissionIntensityPicker.OnValueChange += (emissionIntensity) =>
        {
            OnMaterialEdited?.Invoke((palette.GetMaterial(selectedMaterial) with { emissionIntensity = emissionIntensity }, selectedMaterial));
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

    [ImportEvent("engine_state_events", "main_voxelPalette_changed")]
    public void OnPaletteChanged(object newPalette)
    {
        palette = newPalette as VoxelPalette ?? throw new ArgumentNullException(nameof(newPalette));
    }

    [ImportEvent("user_voxelTool_events", "activeMaterial_changed")]
    public void SelectMaterial(int newMaterialIndex)
    {
        selectedMaterial = newMaterialIndex;
        VoxelMaterial material = palette.GetMaterial(newMaterialIndex);
        baseColorPicker.color = material.baseColor.AsNum();
        baseColorVariancePicker.Value = material.baseColorVariance;
        emissionPicker.color = material.emissionColor.AsNum();
        emissionIntensityPicker.Value = material.emissionIntensity;
    }
}
public sealed class UI_SettingsWindow : UI_Window
{
    bool UseTAA = true;

    [ExportEvent("ui_events", "renderSettings_TAA_switched")]
    public event Action<object>? OnTAASwitched;

    protected override void Display()
    {
        ImGui.Begin("Render settings");

        ImGui.SeparatorText("Settings");
        if (ImGui.RadioButton("Use TAA", UseTAA))
        {
            UseTAA = !UseTAA;
            OnTAASwitched?.Invoke(UseTAA);
        }

        ImGui.End();
    }
}

public sealed class UI_DebugWindow : UI_Window
{
    VoxelMap voxelMap;
    List<UI_TableColumnInfo> columnInfos;

    public UI_DebugWindow(VoxelMap map)
    {
        voxelMap = map;
    }
    
    [ImportEvent("engine_state_events", "main_voxelMap_changed")]
    public void OnVoxelMapChanged(object newVoxelMap)
    {
        voxelMap = (newVoxelMap as ChunkMap ?? throw new ArgumentNullException(nameof(newVoxelMap))).RawStructure;
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
        ImGui.Text($"RAM usage: {Math.Round(Util.BytesConverter(voxelMap.GetMemoryUsage(), 2), 3)} mb");
        ImGui.Text($"VRAM usage: {Math.Round(Util.BytesConverter(voxelMap.GetGraphicsMemoryUsage(), 2), 3)} mb");

        ImGui.End();
    }
}

public sealed class UI_VoxelToolSettingsWindow : UI_Window
{
    VoxelTool tool;
    InspectorMenu toolConfigMenu;
    public UI_VoxelToolSettingsWindow(VoxelTool tool)
    {
        this.tool = tool;
        tool.OnActivePlacementTechniqueChanged += () =>
        {
            try
            {
                toolConfigMenu = new(tool.ActivePlacementTechnique);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        };
    }

    protected override void Display()
    {
        ImGui.Begin("Voxel tool");

        ImGui.SeparatorText("Modes");

        if (ImGui.BeginCombo("", tool.ActivePlacementTechnique.Name))
        {
            for (int i = 0; i < tool.TechniqueCount; i++)
            {
                if (ImGui.Selectable(tool[i].Name, i == tool.ActivePlacementTechniqueIndex))
                {
                    tool.ActivePlacementTechniqueIndex = i;
                }
            }
            ImGui.EndCombo();
        }

        if (tool.ActivePlacementTechnique is ComplexPlacementTechnique)
        {
            ImGui.TextColored(new Vector4(1, 0.8078f, 0.2784f, 1), "Enter - apply\nBackspace - cancel");
        }

        ImGui.SeparatorText("Settings");

        toolConfigMenu.Display();

        ImGui.End();
    }
}
