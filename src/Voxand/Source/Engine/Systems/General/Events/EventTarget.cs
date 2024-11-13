using System.Reflection;


namespace Voxand.Engine.Systems.General.Events;
public class EventTarget
{
    Dictionary<string, List<Action<object>>> events = [];
    public void AddEvent(string eventName)
    {
        if (HasEvent(eventName))
            throw new ArgumentException($"Cannot add an event. Event {eventName} already exists");
        events[eventName] = new();
    }
    public void AddEvent(object target, EventInfo eventInfo, string eventName)
    {
        AddEvent(eventName);
        BindInvokation(target, eventInfo, eventName);
    }
    public void BindInvokation(object target, EventInfo eventInfo, string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventInfo); ArgumentNullException.ThrowIfNull(target);

        Action<EventArgs> handlerAction = (args) => Invoke(eventName, args);
        Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, handlerAction.Target, handlerAction.Method);
        eventInfo.AddEventHandler(target, handler);
    }
    public void Subscribe(string eventName, Action<object> action)
    {
        if (!HasEvent(eventName))
            throw new ArgumentException($"Cannot subscribe to an event. Event {eventName} does not exists");
        events[eventName].Add(action);
    }
    public void Unsubscribe(string eventName, Action<object> action)
    {
        if (!HasEvent(eventName))
            throw new ArgumentException($"Cannot subscribe to an event. Event {eventName} does not exists");
        events[eventName].Remove(action);
    }
    public void Invoke(string eventName, EventArgs args)
    {
        if (!HasEvent(eventName))
            throw new ArgumentOutOfRangeException($"Cannot invoke an event. Event {eventName} does not exists");

        var subscribers = events[eventName];
        foreach (Action<EventArgs> subscriberAction in subscribers)
            subscriberAction.Invoke(args);
    }
    public bool HasEvent(string eventName) => events.ContainsKey(eventName);
}