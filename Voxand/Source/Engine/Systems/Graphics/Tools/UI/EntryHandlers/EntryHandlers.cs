using Voxand.Engine.Systems.Graphics.Tools.UI;
using Voxand.UI.Components;

public abstract class PropertyEntryHandler()
{
    public abstract void Display();
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class PropertyEntryTypeAttribute(Type type) : Attribute
{
    public Type TargetType { get; } = type;
}



[PropertyEntryType(typeof(float))]
public sealed class FloatEntryHandler : PropertyEntryHandler
{
    FloatPicker entry;
    EntryDescriptor<float> descriptor;
    public FloatEntryHandler(EntryDescriptor<float> descriptor)
    {
        this.descriptor = descriptor;
        entry = new(descriptor.Name);
        entry.value = descriptor.ReferencedProperty.Get();
        entry.OnValueChange += descriptor.ReferencedProperty.Set;
    }
    public override void Display()
    {
        entry.value = descriptor.ReferencedProperty.Get();
        entry.Display();
    }
}
