using System.Reflection;

namespace Voxand.Helpers.Reflection;
public struct RemoteProperty<T>
{
    Func<T> get;
    Action<T> set;
    public RemoteProperty(object target, PropertyInfo propInfo)
    {
        get = (Func<T>)propInfo.GetGetMethod()!.CreateDelegate(typeof(Func<T>), target);
        set = (Action<T>)propInfo.GetSetMethod()!.CreateDelegate(typeof(Action<T>), target);
    }
    public T Get() => get();
    public void Set(T value) => set(value);
}