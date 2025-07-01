using ImGuiNET;

using NVec2 = System.Numerics.Vector2;
namespace Voxand.UI;
public static class UI_Manager
{
    static List<UI_Window> windows;

    public static void Initialize()
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