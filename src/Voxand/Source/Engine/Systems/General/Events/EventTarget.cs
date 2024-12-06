using System.Reflection;


namespace Voxand.Engine.Systems.General.Events;

public interface IEventSubscribable
{
    public void Subscribe(string eventName, Action<object> action);
} 
public class EventTarget : IEventSubscribable
{
    protected Dictionary<string, List<Action<object>>> events = [];
    public void AddEvent(string eventName)
    {
        if (HasEvent(eventName))
            throw new ArgumentException($"Cannot add an event. Event {eventName} already exists.");
        events[eventName] = new();
    }
    public void RemoveEvent(string eventName)
    {
        if (!HasEvent(eventName))
            throw new ArgumentException($"Cannot remove an event. Event {eventName} does not exist.");
        events.Remove(eventName);
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
            throw new ArgumentException($"Cannot subscribe to an event. Event {eventName} does not exist.");
        events[eventName].Add(action);
    }
    public void Unsubscribe(string eventName, Action<object> action)
    {
        if (!HasEvent(eventName))
            throw new ArgumentException($"Cannot subscribe to an event. Event {eventName} does not exist.");
        events[eventName].Remove(action);
    }
    public void Invoke(string eventName, EventArgs args)
    {
        if (!HasEvent(eventName))
            throw new ArgumentOutOfRangeException($"Cannot invoke an event. Event {eventName} does not exist.");

        var subscribers = events[eventName];
        foreach (Action<EventArgs> subscriberAction in subscribers)
            subscriberAction.Invoke(args);
    }
    public bool HasEvent(string eventName) => events.ContainsKey(eventName);
}