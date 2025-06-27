using ImGuiNET;
using OpenTK.Mathematics;
using Voxand.Helpers;
using Voxand.UI.Components;

using NVec2 = System.Numerics.Vector2;

namespace Voxand.Engine.Systems.Graphics.Tools.UI.InspectorComponents;

public abstract class PropertyEntryHandler : UI_Element
{
    public abstract override void Display();
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class PropertyEntryTypeAttribute(Type type) : Attribute
{
    public Type TargetType { get; } = type;
}

public sealed class ArrayEntryHandler : PropertyEntryHandler
{
    PropertyEntryHandler[] elementEntryHandlers;
    string labelText;
    string label;

    public ArrayEntryHandler(PropertyEntryHandler[] elementEntryHandlers, InspectorPropertyAttribute attrib)
    {
        this.elementEntryHandlers = elementEntryHandlers;
        label = attrib.Name;
        labelText = $"{label} ({elementEntryHandlers.Length} items)";
    }

    public override void Display()
    {
        ImGui.Text(labelText);

        NVec2 windowSize = ImGui.GetContentRegionAvail();
        ImGui.BeginChild(label, new NVec2(windowSize.X, 100), ImGuiChildFlags.ResizeY | ImGuiChildFlags.Border);

        for (int i = 0; i < elementEntryHandlers.Length; i++)
        {
            ImGui.Text($"{i}.");
            ImGui.PushID(i);
            elementEntryHandlers[i].Display();
            ImGui.PopID();
            ImGui.Separator();
        }

        ImGui.EndChild();
    }
}


[PropertyEntryType(typeof(int))]
public sealed class IntEntryHandler : PropertyEntryHandler
{
    IntPicker entry;
    EntryDescriptor<int> descriptor;
    float hoverTime = 0;
    public IntEntryHandler(EntryDescriptor<int> descriptor)
    {
        this.descriptor = descriptor;
        entry = new(descriptor.Name);
        entry.Value = descriptor.ReferencedProperty.Get();
        entry.OnValueChange += descriptor.ReferencedProperty.Set;
    }
    public override void Display()
    {
        entry.Value = descriptor.ReferencedProperty.Get();
        entry.Display();
        if (entry.IsHovered)
        {
            hoverTime += FrameTime.Delta;
            if (hoverTime > 0.5f)
            {
                ImGui.BeginTooltip();
                ImGui.Text(descriptor.Description);
                ImGui.EndTooltip();
            }
        }
        else hoverTime = 0;
    }
}


[PropertyEntryType(typeof(float))]
public sealed class FloatEntryHandler : PropertyEntryHandler
{
    FloatPicker entry;
    EntryDescriptor<float> descriptor;
    float hoverTime = 0;
    public FloatEntryHandler(EntryDescriptor<float> descriptor)
    {
        this.descriptor = descriptor;
        entry = new(descriptor.Name);
        entry.Value = descriptor.ReferencedProperty.Get();
        entry.OnValueChange += descriptor.ReferencedProperty.Set;
    }
    public override void Display()
    {
        entry.Value = descriptor.ReferencedProperty.Get();
        entry.Display();
        if (entry.IsHovered)
        {
            hoverTime += FrameTime.Delta;
            if (hoverTime > 0.5f)
            {
                ImGui.BeginTooltip();
                ImGui.Text(descriptor.Description);
                ImGui.EndTooltip();
            }
        }
        else hoverTime = 0;
    }
}

[PropertyEntryType(typeof(Vector3i))]
public sealed class Vector3iEntryHandler : PropertyEntryHandler
{
    Vector3iPicker entry;
    EntryDescriptor<Vector3i> descriptor;
    float hoverTime = 0;
    public Vector3iEntryHandler(EntryDescriptor<Vector3i> descriptor)
    {
        this.descriptor = descriptor;
        entry = new(descriptor.Name);
        entry.Value = descriptor.ReferencedProperty.Get();
        entry.OnValueChange += descriptor.ReferencedProperty.Set;
    }
    public override void Display()
    {
        entry.Value = descriptor.ReferencedProperty.Get();
        entry.Display();
        if (entry.IsHovered)
        {
            hoverTime += FrameTime.Delta;
            if (hoverTime > 0.5f)
            {
                ImGui.BeginTooltip();
                ImGui.Text(descriptor.Description);
                ImGui.EndTooltip();
            }
        }
        else hoverTime = 0;
    }
}