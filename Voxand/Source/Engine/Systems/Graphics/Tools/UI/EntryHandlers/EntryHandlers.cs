using Voxand.Engine.Systems.Graphics.Tools.UI;
using Voxand.UI.Components;

public abstract class PropertyEntryHandler
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
    public FloatEntryHandler(EntryDescriptor descriptor)
    {
        entry = new(descriptor.Name);
        entry.value = (float)descriptor.ReferencedProperty.Get();
        entry.OnValueChange += (value) => descriptor.ReferencedProperty.Set(value);
    }
    public override void Display()
    {
        entry.Display();
    }
}
