using System.Reflection;
using Voxand.Helpers.Reflection;

namespace Voxand.Engine.Systems.Graphics.Tools.UI;
public class PropertyConfigMenu
{
    static Dictionary<Type, Type> entryHandlerTypes = [];
    static PropertyConfigMenu()
    {
        Type[] rawEntryHandlerTypes = ReflectionHelper.GetDerivedTypes<PropertyEntryHandler>();
        foreach (Type type in rawEntryHandlerTypes)
        {
            var attrib = type.GetCustomAttribute<PropertyEntryTypeAttribute>() ?? 
                throw new InvalidOperationException($"A class derived from {nameof(PropertyEntryHandler)} should have an attribute {nameof(PropertyEntryTypeAttribute)}."); ;
                
            entryHandlerTypes[attrib.TargetType] = type;
        }
    }

    List<PropertyEntryHandler> entryHandlers = [];

    public PropertyConfigMenu(object obj)
    {
        ReflectionHelper.ForEachAttribOfEachProperty<ConfigurableAttribute>(obj, BindingFlags.Public | BindingFlags.Instance, (prop, attrib) =>
        {
            entryHandlers.Add(CreateEntryHandler(obj, prop, attrib));
        });
    }

    PropertyEntryHandler CreateEntryHandler(object obj, PropertyInfo prop, ConfigurableAttribute attrib)
    {
        if (!entryHandlerTypes.TryGetValue(prop.PropertyType, out var handlerType))
            throw new ArgumentException($"Property type {prop.PropertyType} (found in {obj}) has no entry handler type associated with it.");

        // Creating property accessor
        Type remotePropertyType = typeof(RemoteProperty<>).MakeGenericType(prop.PropertyType);
        object remoteProp = Activator.CreateInstance(remotePropertyType, [obj, prop]) ??
            throw new InvalidOperationException($"Failed to instantiate remote property of type {remotePropertyType}");

        // Creating entry descriptor
        Type descriptorType = typeof(EntryDescriptor<>).MakeGenericType(prop.PropertyType);
        ConstructorInfo descriptorConstructor = descriptorType.GetConstructor([typeof(string), typeof(string), remotePropertyType]) ??
            throw new InvalidOperationException($"Failed to get entry descriptor constructor of type {descriptorType}");

        object descriptor = descriptorConstructor.Invoke([attrib.Name, attrib.Description, remoteProp]) ?? 
            throw new InvalidOperationException($"Failed to instantiate entry descriptor of type {descriptorType}"); ;

        // Creating entry handler
        PropertyEntryHandler handlerObject = Activator.CreateInstance(handlerType, [descriptor]) as PropertyEntryHandler ??
            throw new InvalidOperationException($"Failed to instantiate entry handler of type {handlerType}");

        return handlerObject;
    }

    public void Display()
    {
        foreach (var entryHandler in entryHandlers)
            entryHandler.Display();
    }
}

public struct EntryDescriptor<T>(string name, string description, RemoteProperty<T> property)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public RemoteProperty<T> ReferencedProperty { get; set; } = property;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ConfigurableAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}