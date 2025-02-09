using System.Reflection;

namespace Voxand.Helpers.Reflection;
public struct RemoteProperty
{
    object target;
    PropertyInfo propInfo;
    public RemoteProperty(object target, PropertyInfo propInfo)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(propInfo);

        this.target = target;
        this.propInfo = propInfo;
    }
    public object Get() => propInfo.GetValue(target)!;
    public void Set(object value) => propInfo.SetValue(target, value);
    public Type PropertyType => propInfo.PropertyType;
}