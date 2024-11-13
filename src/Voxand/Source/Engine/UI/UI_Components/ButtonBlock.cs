using ImGuiNET;
using OpenTK.Compute.OpenCL;
using System.Numerics;

namespace Voxand.UI.Components;

public class ButtonBlock : UI_Element
{
    int numberOfElements;
    List<UI_Button> elements;
    float rowLength;
    bool adapt;
    public ButtonBlock(List<UI_Button> elements)
    {
        this.elements = elements;
        numberOfElements = elements.Count;
        adapt = true;
    }
    public ButtonBlock(List<UI_Button> elements, int rowLength)
    {
        this.elements = elements;
        numberOfElements = elements.Count;
        this.rowLength = rowLength;
        adapt = false;
    }
    public override void Display()
    {
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        if (adapt) rowLength = ImGui.GetWindowSize().X;
        int i = 0;
        float l = rowLength - spacing;
        while (i < numberOfElements)
        {
            float size = elements[i].size.X + spacing;
            l -= size;
            if (l < 0)
            {
                ImGui.NewLine(); elements[i].Display(); ImGui.SameLine();
                l = rowLength - size - spacing;
            }
            else
            {
                elements[i].Display();
                ImGui.SameLine();
            }
            i++;
        }
    }
}