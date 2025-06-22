using System.Reflection;
using Voxand.Helpers.Reflection;
using Voxand.Source.Engine.Systems.Graphics.Tools.UI.InspectorComponents;
using Voxand.UI.Components;

namespace Voxand.Engine.Systems.Graphics.Tools.UI;
public class InspectorMenu
{
    static Dictionary<Type, Type> entryHandlerTypes = [];
    static InspectorMenu()
    {
        foreach (Type type in ReflectionHelper.GetDerivedTypes<PropertyEntryHandler>())
        {
            var attrib = type.GetCustomAttribute<PropertyEntryTypeAttribute>();
            
            if (attrib is not null)
                entryHandlerTypes[attrib.TargetType] = type;
        }
    }

    List<UI_Element> components = [];

    public InspectorMenu(object obj)
    {
        HashSet<string> componentNames = new();
        PriorityQueue<UI_Element, int> componentOrderQueue = new();

        ReflectionHelper.ForEachAttribOfEachMember<InspectorPropertyAttribute>(obj, BindingFlags.Public | BindingFlags.Instance, (memberInfo, attrib) =>
        {
            if (componentNames.Contains(attrib.Name))
                throw new ArgumentException($"Property name {attrib.Name} for inspector menu generation is used in multiple properies of {obj.GetType()}.");
            componentNames.Add(attrib.Name);

            componentOrderQueue.Enqueue(memberInfo switch
            {
                PropertyInfo propInfo => CreateEntryHandler(obj, propInfo, attrib),
                MethodInfo methodInfo => CreateMethodButton(obj, methodInfo, attrib),

                _ => throw new ArgumentException($"Member {memberInfo.Name} ({memberInfo.GetType().Name}; found in {obj.GetType()}) is not supported for inspector menu generation.")
            }, attrib.DisplayPriority);
        });

        while (componentOrderQueue.TryDequeue(out UI_Element? component, out _))
            components.Add(component);
    }

    PropertyEntryHandler CreateEntryHandler(object obj, PropertyInfo prop, InspectorPropertyAttribute attrib)
    {
        Type entryHandlerType, remotePropertyAccessorType;
        object remoteProp;

        if (prop.PropertyType.IsArray)
            return CreateArrayEntryHandler(obj, prop, attrib);

        if (!entryHandlerTypes.TryGetValue(prop.PropertyType, out entryHandlerType!))
            throw new ArgumentException($"Property type {prop.PropertyType} (found in {obj.GetType()}) has no entry handler type associated with it.");

        // Creating property accessor
        remotePropertyAccessorType = typeof(RemoteProperty<>).MakeGenericType(prop.PropertyType);
        remoteProp = Activator.CreateInstance(remotePropertyAccessorType, [obj, prop]) ??
            throw new InvalidOperationException($"Failed to instantiate remote property of type {remotePropertyAccessorType}");

        return CreateSingleEntryHandler(prop.PropertyType, entryHandlerType, remoteProp, attrib);
    }

    static ArrayEntryHandler CreateArrayEntryHandler(object obj, PropertyInfo prop, InspectorPropertyAttribute attrib)
    {
        Type elementType = prop.PropertyType.GetElementType()!;

        if (!entryHandlerTypes.TryGetValue(elementType, out Type? entryHandlerType))
            throw new ArgumentException($"Array element type {elementType} (found in an array {prop.Name} of {obj}) has no entry handler type associated with it.");

        object arrayObj = prop.GetValue(obj)!;

        int elementCount = ((Array)arrayObj).Length;
        PropertyEntryHandler[] elementEntryHandlers = new PropertyEntryHandler[elementCount];

        InspectorPropertyAttribute elementAttrib = new(string.Empty, attrib.Description, 0);

        for (int i = 0; i < elementCount; i++)
        {
            MethodInfo createRemoteArrayElementAccessorMethod = typeof(InspectorMenu).GetMethod(nameof(CreateRemoteArrayElementAccessor), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(elementType);

            object elementAccessor = createRemoteArrayElementAccessorMethod.Invoke(null, [arrayObj, i]) ??
                throw new InvalidOperationException($"Failed to instantiate remote property for array element access.");

            elementEntryHandlers[i] = CreateSingleEntryHandler(elementType, entryHandlerType, elementAccessor, elementAttrib);
        }

        return new ArrayEntryHandler(elementEntryHandlers, attrib);
    }
    static RemoteProperty<T> CreateRemoteArrayElementAccessor<T>(object arrayObj, int index)
    {
        T[] array = arrayObj as T[] ??
            throw new ArgumentException($"Expected {nameof(arrayObj)} to be an array of type {typeof(T)} but got {arrayObj.GetType()}.");

        Func<T> getter = () =>
        {
            return array[index];
        };

        Action<T> setter = (value) =>
        {
            array[index] = value;
        };

        return new RemoteProperty<T>(getter, setter);
    }

    static PropertyEntryHandler CreateSingleEntryHandler(Type propertyType, Type entryHandlerType, object remotePropertyAccessor, InspectorPropertyAttribute attrib)
    {
        object descriptor = CreateEntryDescriptor(propertyType, remotePropertyAccessor, attrib);

        return Activator.CreateInstance(entryHandlerType, [descriptor]) as PropertyEntryHandler ??
            throw new InvalidOperationException($"Failed to instantiate entry handler of type {entryHandlerType}");
    }

    static object CreateEntryDescriptor(Type propertyType, object remotePropertyAccessor, InspectorPropertyAttribute attrib)
    {
        Type descriptorType = typeof(EntryDescriptor<>).MakeGenericType(propertyType);
        ConstructorInfo descriptorConstructor = descriptorType.GetConstructor([typeof(string), typeof(string), remotePropertyAccessor.GetType()]) ??
            throw new InvalidOperationException($"Failed to get entry descriptor constructor of type {descriptorType}");

        return descriptorConstructor.Invoke([attrib.Name, attrib.Description, remotePropertyAccessor]) ??
            throw new InvalidOperationException($"Failed to instantiate entry descriptor of type {descriptorType}");
    }

    static UI_Button CreateMethodButton(object obj, MethodInfo methodInfo, InspectorPropertyAttribute attrib)
    {
        UI_Button button = new UI_Button(attrib.Name, new System.Numerics.Vector2(100, 50))
        {
            Description = attrib.Description,
            SizeMode = SizeMode.FillWidth
        };
        
        Action methodDelegate = methodInfo.CreateDelegate<Action>(obj) ??
            throw new InvalidOperationException($"Failed to create delegate for method {methodInfo.Name} of type {methodInfo.DeclaringType}");

        button.OnClick += methodDelegate;

        return button;
    }

    public void Display()
    {
        foreach (var component in components)
            component.Display();
    }
}

public struct EntryDescriptor<T>(string name, string description, RemoteProperty<T> property)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public RemoteProperty<T> ReferencedProperty { get; set; } = property;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
public sealed class InspectorPropertyAttribute(string name, string description, int displayPriority) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public int DisplayPriority { get; } = displayPriority;
}