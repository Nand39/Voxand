using System.Numerics;

using ImGuiNET;

using Voxand.Content;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.UI.Components;
using Voxand.Engine.Systems.General.Events;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.UI;
public static class UI_Manager
{
    static List<UI_Window> windows;

    public static EventDispatcher Events = new("ui_events");
    public static void Initialize(ContentManager content)
    {
        windows = new List<UI_Window>();

        string styleJSON = content.ReadFile("Graphics/UI/ImGuiStyles/style.json", out bool status);
        if (status) 
            ImGuiController.SetStyle(styleJSON);
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
    List<UI_ColorButton> paletteButtons;
    ButtonBlock materialSelector;
    VoxelPalette palette;
    public int SelectedMaterial { get; private set; } = 0;

    public UI_PaletteWindow(VoxelPalette palette)
    {
        this.palette = palette;
        BuildPaletteUI(palette);
    }

    [ExportEvent("ui_events", "voxel_material_selected")] 
    public event Action<object> OnMaterialSelected;

    [ImportEvent("engine_state_events", "main_voxelPalette_changed")]
    public void SetPalette(object newPalette)
    {
        palette = newPalette as VoxelPalette ?? throw new ArgumentNullException(nameof(newPalette));

        BuildPaletteUI(palette);
    }
    void BuildPaletteUI(VoxelPalette palette)
    {
        paletteButtons = new(palette.MaterialCount);
        for (int i = 0; i < palette.MaterialCount; i++)
        {
            UI_ColorButton button = new(new Vector2(30), i);
            button.Data["id"] = i;
            button.color = new Vector4(Util.GetMaterialSolidColor(palette.GetMaterial(i)).AsNum(), 1);
            button.hoverColor = button.color;
            button.outlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));
            button.outlineThickness = 2;

            button.OnEnable += (data) => OnMaterialSelected?.Invoke((int)data["id"]);

            paletteButtons.Add(button);
        }

        List<UI_Button> buttons = new();
        for (int i = 0; i < paletteButtons.Count; i++)
            buttons.Add(paletteButtons[i]);
        materialSelector = new ButtonBlock(buttons);
    }
    protected override void Display()
    {
        ImGui.Begin("Palette");
        materialSelector.Display();
        ImGui.End();
    }

    [ImportEvent("voxelPalette_events", "material_modified")]
    public void UpdatePaletteButton(int updatedMaterialIndex)
    {
        Vector4 newColor = new Vector4(Util.GetMaterialSolidColor(palette.GetMaterial(updatedMaterialIndex)).AsNum(), 1);
        UI_Button button = paletteButtons[updatedMaterialIndex];
        button.color = newColor;
        button.hoverColor = newColor;
    }

    public void Select(int newActiveMaterialIndex)
    {
        Select(newActiveMaterialIndex, false);
    }

    [ImportEvent("user_voxelTool_events", "activeMaterial_changed")]
    public void SelectSilent(int newActiveMaterialIndex)
    {
        Select(newActiveMaterialIndex, true);
    }
    void Select(int newActiveMaterialIndex, bool silent)
    {
        paletteButtons[SelectedMaterial].Disable(silent);
        paletteButtons[newActiveMaterialIndex].Enable(silent);
        SelectedMaterial = newActiveMaterialIndex;
    }
}

public sealed class UI_MaterialEditorWindow : UI_Window
{
    ColorPicker colorPicker;
    ColorPicker emissionPicker;
    FloatPicker emissionIntensityPicker;
    VoxelPalette palette;
    int selectedMaterial;

    [ExportEvent("ui_events", "voxel_material_edited")] 
    public event Action<object>? OnMaterialEdited;

    public UI_MaterialEditorWindow(VoxelPalette palette)
    {
        this.palette = palette;

        colorPicker = new("Voxel color");
        emissionPicker = new("Voxel emission");
        emissionIntensityPicker = new("Emission intensity");

        colorPicker.OnColorChange += (color) => SetMaterialColor(color, selectedMaterial);
        emissionPicker.OnColorChange += (color) => SetMaterialEmission(color * emissionIntensityPicker.value, selectedMaterial);
        emissionIntensityPicker.OnValueChange += (value) => SetMaterialEmission(emissionPicker.color * value, selectedMaterial);
    }
    protected override void Display()
    {
        ImGui.Begin("Material properties");
        colorPicker.Display();
        emissionPicker.Display();
        emissionIntensityPicker.Display();
        ImGui.End();
    }

    [ImportEvent("engine_state_events", "main_voxelPalette_changed")]
    public void OnPaletteChanged(object newPalette)
    {
        palette = newPalette as VoxelPalette ?? throw new ArgumentNullException(nameof(newPalette));
    }
    void SetMaterialColor(Vector3 newColor, int index)
    {
        VoxelMaterial oldMaterial = palette.GetMaterial(index);
        VoxelMaterial newMaterial = new VoxelMaterial(
            color: newColor.AsTK(),
            emission: oldMaterial.emission);
        OnMaterialEdited?.Invoke((newMaterial, selectedMaterial));
    }
    void SetMaterialEmission(Vector3 newEmission, int index)
    {
        VoxelMaterial oldMaterial = palette.GetMaterial(index);
        VoxelMaterial newMaterial = new VoxelMaterial(
            color: oldMaterial.color,
            emission: newEmission.AsTK());
        OnMaterialEdited?.Invoke((newMaterial, selectedMaterial));
    }

    [ImportEvent("user_voxelTool_events", "activeMaterial_changed")]
    public void SelectMaterial(int newMaterialIndex)
    {
        selectedMaterial = newMaterialIndex;
        VoxelMaterial material = palette.GetMaterial(newMaterialIndex);
        colorPicker.color = material.color.AsNum();
        float emissionScaler = material.emission.Max();
        emissionPicker.color = emissionScaler > 1 ? material.emission.AsNum() / emissionScaler : material.emission.AsNum();
        emissionIntensityPicker.value = emissionScaler;
    }
}
public sealed class UI_SettingsWindow : UI_Window
{
    protected override void Display()
    {
        
    }
}

public sealed class UI_DebugWindow : UI_Window
{
    VoxelMap voxelMap;
    public UI_DebugWindow(VoxelMap map) => voxelMap = map;
    
    [ImportEvent("engine_state_events", "main_voxelMap_changed")]
    public void OnVoxelMapChanged(object newVoxelMap)
    {
        voxelMap = newVoxelMap as VoxelMap ?? throw new ArgumentNullException(nameof(newVoxelMap));
    }
    protected override void Display()
    {
        ImGui.Begin("Debug");

        ImGui.SeparatorText("Frame time");
        ImGui.Text($"FPS: {Util.FrameTimeData.FPS}");
        ImGui.Text($"Avg: {Util.FrameTimeData.AvgTime.TotalMilliseconds} ms");
        ImGui.Text($"Max: {Util.FrameTimeData.MaxTime.TotalMilliseconds} ms");
        ImGui.Text($"Min: {Util.FrameTimeData.MinTime.TotalMilliseconds} ms");

        ImGui.SeparatorText("Memory usage");
        ImGui.Text($"RAM usage: {Math.Round(Util.BytesConverter(voxelMap.GetMemoryUsage(), 2), 3)} mb");
        ImGui.Text($"VRAM usage: {Math.Round(Util.BytesConverter(voxelMap.GetGraphicsMemoryUsage(), 2), 3)} mb");

        ImGui.End();
    }
}