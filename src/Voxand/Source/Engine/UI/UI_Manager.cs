using System.Numerics;
using System.Reflection;

using ImGuiNET;

using Voxand.Content;
using Voxand.Engine.Graphics;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.UI.Components;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.UI;
public static class EventTransformer
{
    delegate void UIRequestHandler(EventArgs args);
    public static void AddEvents(this EventTarget manager, UI_Window window)
    {
        Type T = window.GetType();
        EventInfo[] events = T.GetEvents();
        foreach (EventInfo @event in events)
        {
            if (@event.GetCustomAttribute(typeof(WindowEventAttribute)) == null) continue;
            manager.AddEvent(window, @event, @event.Name);
        }
    }
}

[AttributeUsage(AttributeTargets.Event)]
public sealed class WindowEventAttribute : Attribute;
class OstapEventArgs : EventArgs
{
    public string text = "Hi!";
}
class Test : UI_Window
{
    [WindowEvent] public event Action<EventArgs> action;
    protected override void Display()
    {
        EventArgs args = new OstapEventArgs();
        action?.Invoke(args);
    }
}

public class UI_Manager
{
    UI_Button saveMapBtn, loadMapBtn, confirmMapNameBtn;
    ButtonBlock saveLoad;

    List<UI_Window> windows;

    VoxelPalette voxelPalette;

    string exportName = "";

    public static EventTarget Events = new();
    public UI_Manager(VoxelPalette voxelPalette, ContentManager content)
    {

        //windows = new List<UI_Window>()
        //{
        //    new UI_Window(materialPropertiesElements, (elements) =>
        //    {

        //    }),
        //    new UI_Window(, (elements) =>
        //    {
        //        ImGui.SetNextWindowPos(Vector2.Zero);
        //        ImGui.SetNextWindowSize(new Vector2(Util.ClientSize.X, 60));
        //        ImGui.Begin("Top bar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNav);
        //        ImGui.Text("It's a top bar!");
        //        ImGui.End();
        //    }),
        //    new UI_Window((elements) =>
        //    {
        //        ImGui.Begin("Render settings");

        //        ImGui.End();
        //    }),
        //};

        this.voxelPalette = voxelPalette;
        var paletteWindow = new UI_PaletteWindow(voxelPalette);
        var materialEditorWindow = new UI_MaterialEditorWindow(voxelPalette);
        var renderSettingsWindow = new UI_SettingsWindow();
        var debugWindow = new UI_DebugWindow();

        paletteWindow.OnMaterialSelectionRequest += (material) =>
        {
            materialEditorWindow.SelectMaterial(material);
            ActiveState.material = (uint)material;
        };
        materialEditorWindow.OnMaterialColorChangeRequest += (color, materialIndex) =>
        {
            SetMaterialColor(color, materialIndex);
            paletteWindow.UpdatePaletteButton(materialIndex);
        };
        materialEditorWindow.OnMaterialEmissionChangeRequest += (emission, materialIndex) =>
        {
            SetMaterialEmission(emission, materialIndex);
            paletteWindow.UpdatePaletteButton(materialIndex);
        };
        renderSettingsWindow.OnRenderTechniqueChangeRequest += (technique) => { OnRenderTechniqueChangeRequest?.Invoke(technique); };
        //renderSettingsWindow.OnPrintingSpeedChangeRequest += (speed) => { ActiveState.printingSpeed = speed; };

        paletteWindow.Select(0);
        Test test = new Test();
        Events.AddEvents(test);
        windows = 
            [
            paletteWindow,
            materialEditorWindow,
            renderSettingsWindow,
            debugWindow,
            test,
            ];

        //saveMapBtn = new UI_Button("Save map", new Vector2(80, 40)) 
        //    { color = new Vector4(0.2f, 0.2f, 0.2f, 0.2f), hoverColor = new Vector4(0.2f, 0.2f, 0.2f, 0.6f) };
        //loadMapBtn = new UI_Button("Load map", new Vector2(80, 40))
        //    { color = new Vector4(0.2f, 0.2f, 0.2f, 0.2f), hoverColor = new Vector4(0.2f, 0.2f, 0.2f, 0.6f) };
        //saveLoad = new ButtonBlock(new() { saveMapBtn, loadMapBtn });

        //saveMapBtn.OnClick += (data) =>
        //{
        //    OnMapSave?.Invoke();
        //};
        //loadMapBtn.OnClick += (data) =>
        //{
        //    OnMapLoad?.Invoke(exportName);
        //};

        //confirmMapNameBtn = new UI_Button("Confirm", new Vector2(80, 40))
        //    { color = new Vector4(0.2f, 0.2f, 0.2f, 0.2f), hoverColor = new Vector4(0.2f, 0.2f, 0.2f, 0.6f) };



        //confirmMapNameBtn.OnClick += (data) => OnMapNameChange?.Invoke(exportName);
        string styleJSON = content.ReadFile("Graphics/UI/ImGuiStyles/style.json", out bool status);
        if (status) ImGuiController.SetStyle(styleJSON);
    }

    public event Action OnMapSaveRequest;
    public event Action<string> OnMapLoadRequest;
    public event Action<string> OnMapNameChangeRequest;
    public event Action<RenderTechniques> OnRenderTechniqueChangeRequest;
    void SetMaterialColor(Vector3 newColor, int index)
    {
        VoxelMaterial oldMaterial = voxelPalette.GetMaterial(index);
        VoxelMaterial newMaterial = new VoxelMaterial(
            color: newColor.AsTK(),
            emission: oldMaterial.emission);
        voxelPalette.SetMaterial(index, newMaterial);
    }
    void SetMaterialEmission(Vector3 newEmission, int index)
    {
        VoxelMaterial oldMaterial = voxelPalette.GetMaterial(index);
        VoxelMaterial newMaterial = new VoxelMaterial(
            color: oldMaterial.color,
            emission: newEmission.AsTK());
        voxelPalette.SetMaterial(index, newMaterial);
    }
    public void Display()
    {

        ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(mainViewport.Pos);
        ImGui.SetNextWindowSize(mainViewport.Size);
        ImGui.SetNextWindowViewport(mainViewport.ID);

        ImGui.Begin("MainDockSpace", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground);
        ImGui.DockSpace(ImGui.GetID("MainDockSpaceID"), Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.End();

        //uint dockspaceID = ImGui.GetID("MainDockSpace");
        //ImGui.SetNextWindowDockID(dockspaceID, ImGuiCond.Always);

        foreach (UI_Window window in windows)
            window.ShowIfVisible();

        //ImGui.Begin("Edit map name", ImGuiWindowFlags.NoCollapse);
        //ImGui.InputText("Map name", ref exportName, 64);
        //confirmMapNameBtn.Display();
        //ImGui.End();

        //ImGui.Begin("Save/load map");
        //saveLoad.Display();
        //ImGui.End();
    }
    public void AddWindow(UI_Window window)
    {
        Events.AddEvents(window);
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
    VoxelPalette voxelPalette;
    List<UI_ColorButton> paletteButtons;
    ButtonBlock voxelSelector;
    public int selectedMaterial { get; private set; } = 0;

    public event Action<int>? OnMaterialSelectionRequest;
    public UI_PaletteWindow(VoxelPalette voxelPalette)
    {
        this.voxelPalette = voxelPalette;
        paletteButtons = new(voxelPalette.PaletteLength);
        for (int i = 0; i < voxelPalette.PaletteLength; i++)
        {
            UI_ColorButton button = new(new Vector2(30), i, i);
            button.Data["id"] = i;
            button.color = new Vector4(Util.GetMaterialSolidColor(voxelPalette.GetMaterial(i)).AsNum(), 1);
            button.hoverColor = button.color;
            button.outlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));
            button.outlineThickness = 2;

            button.OnClick += (data, key, selected) =>
            {
                paletteButtons[selectedMaterial].selected = false;
                selectedMaterial = data["id"];
                paletteButtons[selectedMaterial].selected = true;
                OnMaterialSelectionRequest?.Invoke(selectedMaterial);
            };
            paletteButtons.Add(button);
        }

        List<UI_Button> buttons = new();
        for (int i = 0; i < paletteButtons.Count; i++) 
            buttons.Add(paletteButtons[i]);
        voxelSelector = new ButtonBlock(buttons);
    }
    protected override void Display()
    {
        ImGui.Begin("Palette");
        voxelSelector.Display();
        ImGui.End();
    }
    public void UpdatePaletteButton(int updatedMaterial)
    {
        Vector4 newColor = new Vector4(Util.GetMaterialSolidColor(voxelPalette.GetMaterial(updatedMaterial)).AsNum(), 1);
        UI_Button button = paletteButtons[updatedMaterial];
        button.color = newColor;
        button.hoverColor = newColor;
    }

    public void Select(int materialIndex)
    {
        paletteButtons[materialIndex].Click();
    }
}

public sealed class UI_MaterialEditorWindow : UI_Window
{
    ColorPicker colorPicker;
    ColorPicker emissionPicker;
    FloatPicker emissionIntensityPicker;
    VoxelPalette palette;
    public int selectedMaterial { get; private set; }

    public event Action<Vector3, int>? OnMaterialColorChangeRequest;
    public event Action<Vector3, int>? OnMaterialEmissionChangeRequest;
    public UI_MaterialEditorWindow(VoxelPalette palette)
    {
        this.palette = palette;

        colorPicker = new("Voxel color");
        emissionPicker = new("Voxel emission");
        emissionIntensityPicker = new("Emission intensity");

        colorPicker.OnColorChange += (color) => OnMaterialColorChangeRequest?.Invoke(color, selectedMaterial);
        emissionPicker.OnColorChange += (color) => OnMaterialEmissionChangeRequest?.Invoke(color * emissionIntensityPicker.value, selectedMaterial);
        emissionIntensityPicker.OnValueChange += (value) => OnMaterialEmissionChangeRequest?.Invoke(emissionPicker.color * value, selectedMaterial);
    }
    protected override void Display()
    {
        ImGui.Begin("Material properties");
        colorPicker.Display();
        emissionPicker.Display();
        emissionIntensityPicker.Display();
        ImGui.End();
    }
    public void SelectMaterial(int materialIndex)
    {
        selectedMaterial = materialIndex;
        VoxelMaterial material = palette.GetMaterial(materialIndex);
        colorPicker.color = material.color.AsNum();
        emissionPicker.color = material.emission.AsNum();
    }
}
public sealed class UI_SettingsWindow : UI_Window
{
    bool useCompute = true;

    [WindowEvent] public event Action<RenderTechniques>? OnRenderTechniqueChangeRequest;
    protected override void Display()
    {
        ImGui.Begin("Render settings");
        if (ImGui.Checkbox("Use compute shaders", ref useCompute)) 
        {
            if (useCompute) OnRenderTechniqueChangeRequest?.Invoke(RenderTechniques.PathTracingCompute);
            else OnRenderTechniqueChangeRequest?.Invoke(RenderTechniques.PathTracingFragment);
        }
        ImGui.End();
    }
}

public sealed class UI_DebugWindow() : UI_Window
{
    protected override void Display()
    {

        ImGui.Begin("Debug");

        ImGui.Text($"Cam pos: {Camera.position}");

        ImGui.SeparatorText("Frame time");
        ImGui.Text($"FPS: {Util.FrameTimeData.FPS}");
        ImGui.Text($"Avg: {Util.FrameTimeData.AvgTime.TotalMilliseconds} ms");
        ImGui.Text($"Max: {Util.FrameTimeData.MaxTime.TotalMilliseconds} ms");
        ImGui.Text($"Min: {Util.FrameTimeData.MinTime.TotalMilliseconds} ms");

        ImGui.SeparatorText("Memory usage");
        ImGui.Text($"RAM usage: {Math.Round(Util.BytesConverter(Util.CurrentMap.GetMemoryUsage(), 2), 3)} mb");
        ImGui.Text($"VRAM usage: {Math.Round(Util.BytesConverter(Util.CurrentMap.GetGraphicsMemoryUsage(), 2), 3)} mb");

        ImGui.End();
    }
}