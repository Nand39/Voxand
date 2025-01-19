using System.Diagnostics;
using System.Reflection;

using Voxand.Helpers.Reflection;

namespace Voxand.Engine.Systems.General.Events;

/// <summary>
/// Allows to pass <see cref="EventDispatcher"/> for objects to subscribe to its events without exposing its entire functionality.
/// </summary>
public interface IEventSubscribable
{
    public void Subscribe(string eventName, Action<object> action);
}

/// <summary>
/// Allows event to be imported to <see cref="EventDispatcher"/> if its tag matches <paramref name="tag"/>. 
/// The event will invoke <see cref="EventDispatcher"/> event whose name is <paramref name="eventName"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Event, AllowMultiple = true)]
public sealed class ExportEventAttribute(string tag, string eventName) : Attribute
{
    public string EventName { get; } = eventName;
    public string Tag { get; } = tag;
}

/// <summary>
/// Allows event to be exported to <see cref="EventDispatcher"/> if its tag matches <paramref name="tag"/>. 
/// Method will be subscribed to the event whose name is <paramref name="eventName"/>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ImportEventAttribute(string tag, string eventName) : Attribute
{
    public string EventName { get; } = eventName;
    public string Tag { get; } = tag;
}

public class EventDispatcher : IEventSubscribable
{
    static HashSet<string> tags = new();
    readonly string tag;
    protected Dictionary<string, List<Action<object>>> events = [];
    public string Tag => tag;

    public EventDispatcher(string tag)
    {
        if (tags.Contains(tag))
            throw new ArgumentException($"{nameof(EventDispatcher)} with tag {tag} already exists.");
        this.tag = tag;
    }

    #region Add/Remove event

    /// <summary>
    /// Adds an event named <paramref name="eventName"/> to the register. Throws <see cref="InvalidOperationException"/> if event already exists.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddEvent(string eventName)
    {
        if (HasEvent(eventName))
            throw new InvalidOperationException($"Cannot add an event. Event {eventName} already exists.");
        AddEventSilent(eventName);
    }

    /// <summary>
    /// Adds an event named <paramref name="eventName"/> to the register only if this event does not exist.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    public void AddEventIfAbsent(string eventName)
    {
        if (!HasEvent(eventName))
            AddEventSilent(eventName);
    }

    /// <summary>
    /// Adds an event named <paramref name="eventName"/> to the register. If already exists, overwrites it.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    void AddEventSilent(string eventName) => events[eventName] = new();

    /// <summary>
    /// Removes event <paramref name="eventName"/> from the register. 
    /// Throws <see cref="KeyNotFoundException"/> if event does not exist.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    public void RemoveEvent(string eventName)
    {
        if (!HasEvent(eventName))
            throw new KeyNotFoundException($"Cannot remove an event. Event {eventName} does not exist.");
        events.Remove(eventName);
    }
    #endregion

    #region Connection

    /// <summary>
    /// If a standard event of <paramref name="eventSource"/> has <see cref="ExportEventAttribute"/> 
    /// and its Tag property matches Tag of this <see cref="EventDispatcher"/>, a new event handler will be added to it
    /// in order to forward call to <see cref="Invoke(string, object)"/> with an event name specified by the attribute.
    /// </summary>
    /// <param name="eventSource">The event source.</param>
    public void ImportEvents(object eventSource)
    {
        ReflectionHelper.ForEachAttribOfEachEvent<ExportEventAttribute>(eventSource, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, (@event, attrib) => 
        {
            if (attrib.Tag == Tag)
            {
                Console.WriteLine($"Imported {@event.Name} to {tag}.");
                BindToOrAddEvent(eventSource, @event, attrib.EventName);
            }
        });
    }

    /// <summary>
    /// Subscribes each method of <paramref name="destination"/> with an attribute <see cref="ImportEventAttribute"/>
    /// to the event of this <see cref="EventDispatcher"/> whose name is specified by the attribute.
    /// </summary>
    /// <param name="destination">Object whose methods to subscribe.</param>
    public void ExportEvents(object destination)
    {
        ReflectionHelper.ForEachAttribOfEachMethod<ImportEventAttribute>(destination, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, (method, attrib) =>
        {
            if (attrib.Tag == Tag)
            {
                Console.WriteLine($"Exported {method.Name}.");
                Subscribe(attrib.EventName, (args) => {
                    object?[] argsArray = { args };
                    method.Invoke(destination, argsArray);
                });
            }
        });
    }

    /// <summary>
    /// Adds event named <paramref name="eventName"/> if absent, then performs <see cref="BindInvocation(object, EventInfo, string)"/>.
    /// </summary>
    /// <param name="target">The event source.</param>
    /// <param name="eventInfo">Metadata of the standard event whose invocation will invoke the event named <paramref name="eventName"/></param>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    public void BindToOrAddEvent(object target, EventInfo eventInfo, string eventName)
    {
        AddEventIfAbsent(eventName);
        BindInvocation(target, eventInfo, eventName);
    }

    /// <summary>
    /// When <paramref name="target"/> raises its standard event whose metadata is described by <paramref name="eventInfo"/>,
    /// the event named <paramref name="eventName"/> will be invoked as its subscriber.
    /// </summary>
    /// <param name="target">The event source.</param>
    /// <param name="eventInfo">Metadata of the standard event whose invocation will invoke the event named <paramref name="eventName"/></param>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    public void BindInvocation(object target, EventInfo eventInfo, string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventInfo); 
        ArgumentNullException.ThrowIfNull(target);

        Action<object> handlerAction = (args) => Invoke(eventName, args);
        Delegate handler;
        try
        {
            handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, handlerAction.Target, handlerAction.Method);
            eventInfo.AddEventHandler(target, handler);
        }
        catch (Exception e)
        {
            Debugger.Break();
        }
    }
    #endregion

    #region Interaction

    /// <summary>
    /// Adds <paramref name="action"/> to the list of subscribed actions of the event named <paramref name="eventName"/>.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    /// <param name="action">Action to subscribe.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    public void Subscribe(string eventName, Action<object> action)
    {
        if (!HasEvent(eventName))
            throw new KeyNotFoundException($"Cannot subscribe to an event. Event {eventName} does not exist.");
        events[eventName].Add(action);
    }

    /// <summary>
    /// Removes first occurrence of <paramref name="action"/> from the list of subscribed actions of the event 
    /// named <paramref name="eventName"/>.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    /// <param name="action">Action to unsubscribe.</param>
    /// <exception cref="ArgumentException"></exception>
    public void Unsubscribe(string eventName, Action<object> action)
    {
        if (!HasEvent(eventName))
            throw new KeyNotFoundException($"Cannot subscribe to an event. Event {eventName} does not exist.");
        events[eventName].Remove(action);
    }

    /// <summary>
    /// Invokes every action subscribed to the event named <paramref name="eventName"/>.
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    /// <param name="args">Arguments that will be passed to subscribed actions.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Invoke(string eventName, object args)
    {
        if (!HasEvent(eventName))
            throw new KeyNotFoundException($"Cannot invoke an event. Event {eventName} does not exist.");

        Console.WriteLine($"Event invoked. Tag={Tag}; Name={eventName}.");

        var subscribers = events[eventName];
        foreach (Action<object> subscriberAction in subscribers)
            subscriberAction.Invoke(args);
    }

    /// <summary>
    /// Checks if this <see cref="EventDispatcher"/> has an event named <paramref name="eventName"/>. 
    /// </summary>
    /// <param name="eventName">Unique case-sensitive identifier of an event.</param>
    /// <returns><see langword="true"/> if an event named <paramref name="eventName"/> was added prior; otherwise <see langword="false"/>.</returns>
    public bool HasEvent(string eventName) => events.ContainsKey(eventName);
    #endregion
}
