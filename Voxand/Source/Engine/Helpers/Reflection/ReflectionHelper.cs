using System.Reflection;

namespace Voxand.Helpers.Reflection;

public static class ReflectionHelper
{
    public static void ForEachMember(object obj, BindingFlags bindingFlags, Action<MemberInfo> action)
    {
        MemberInfo[] members = obj.GetType().GetMembers(bindingFlags);
        foreach (MemberInfo member in members)
            action(member);
    }
    public static void ForEachMethod(object obj, BindingFlags bindingFlags, Action<MethodInfo> action)
    {
        MethodInfo[] methods = obj.GetType().GetMethods(bindingFlags);
        foreach (MethodInfo method in methods)
            action(method);
    }
    public static void ForEachEvent(object obj, BindingFlags bindingFlags, Action<EventInfo> action)
    {
        EventInfo[] events = obj.GetType().GetEvents(bindingFlags);
        foreach (EventInfo @event in events)
            action(@event);
    }
    public static void ForEachProperty(object obj, BindingFlags bindingFlags, Action<PropertyInfo> action)
    {
        PropertyInfo[] properties = obj.GetType().GetProperties(bindingFlags);
        foreach (PropertyInfo property in properties)
            action(property);
    }
    public static void ForEachAttrib<AttribType>(MemberInfo member, Action<AttribType> action) 
        where AttribType : Attribute
    {
        var attribs = member.GetCustomAttributes<AttribType>();
        foreach (var attrib in attribs)
            action(attrib);
    }

    #region Composed operations
    public static void ForEachAttribOfEachMember<AttribType>(object obj, BindingFlags bindingFlags, Action<MemberInfo, AttribType> action)
        where AttribType : Attribute
    {
        ForEachMember(obj, bindingFlags, (member) =>
            ForEachAttrib<AttribType>(member, (attrib) =>
                action(member, attrib)));
    }
    public static void ForEachAttribOfEachEvent<AttribType>(object obj, BindingFlags bindingFlags, Action<EventInfo, AttribType> action)
            where AttribType : Attribute
    {
        ForEachEvent(obj, bindingFlags, (@event) =>
            ForEachAttrib<AttribType>(@event, (attrib) =>
                action(@event, attrib)));
    }
    public static void ForEachAttribOfEachMethod<AttribType>(object obj, BindingFlags bindingFlags, Action<MethodInfo, AttribType> action)
        where AttribType : Attribute
    {
        ForEachMethod(obj, bindingFlags, (method) =>
            ForEachAttrib<AttribType>(method, (attrib) =>
                action(method, attrib)));
    }
    public static void ForEachAttribOfEachProperty<AttribType>(object obj, BindingFlags bindingFlags, Action<PropertyInfo, AttribType> action)
        where AttribType : Attribute
    {
        ForEachProperty(obj, bindingFlags, (property) =>
            ForEachAttrib<AttribType>(property, (attrib) =>
                action(property, attrib)));
    }
    #endregion

    public static IEnumerable<Type> GetDerivedTypes<T>() where T : class
    {
        Type baseType = typeof(T);
        List<Type> derivedTypes = [];

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type[] typesInAssembly = assembly.GetTypes();

            foreach (Type type in typesInAssembly)
                if (type.IsClass && baseType.IsAssignableFrom(type) && !type.IsAbstract)
                    yield return type;
        }
    }
}