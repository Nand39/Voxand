using System.Reflection;

namespace Voxand.Helpers.Reflection;

public static class ReflectionHelper
{
    public static void ForEachMethod(object obj, BindingFlags bindingFlags, Action<MethodInfo> action)
    {
        MethodInfo[] methods = obj.GetType().GetMethods(bindingFlags);
        foreach (MethodInfo method in methods)
            action(method);
    }
    public static void ForEachEvent(object obj, BindingFlags bindingFlags, Action<EventInfo> action)
    {
        Type t = obj.GetType();
        EventInfo[] events = t.GetEvents(bindingFlags);
        foreach (EventInfo @event in events)
            action(@event);
    }
    public static void ForEachAttrib<AttribType>(MemberInfo member, Action<AttribType> action) 
        where AttribType : Attribute
    {
        var attribs = member.GetCustomAttributes(typeof(AttribType));
        foreach (var attrib in attribs)
            action((AttribType)attrib);
    }

    #region Composed operations
    public static void ForEachAttribOfEachEvent<AttribType>(object obj, BindingFlags bindingFlags, Action<EventInfo, AttribType> action)
            where AttribType : Attribute
    {
        ForEachEvent(obj, bindingFlags, (@event) =>
        {
            ForEachAttrib<AttribType>(@event, (attrib) =>
            {
                action(@event, attrib);
            });
        });
    }
    public static void ForEachAttribOfEachMethod<AttribType>(object obj, BindingFlags bindingFlags, Action<MethodInfo, AttribType> action)
        where AttribType : Attribute
    {
        ForEachMethod(obj, bindingFlags, (method) =>
        {
            ForEachAttrib<AttribType>(method, (attrib) =>
            {
                action(method, attrib);
            });
        });
    }
    #endregion
}