using Voxand.Engine.ExecutionControl;
using Voxand.Helpers.Exceptions;

namespace Voxand.Engine.Systems.Common;
public static class EngineServices
{
    static MainExecutionManager.ServiceRegistry services;
    internal static void Initialize(MainExecutionManager.ServiceRegistry services)
    {
        EngineServices.services = services ?? throw new ArgumentNullException($"{nameof(services)} cannot be null.");
    }
    public static bool TryGetService<I>(out I service) where I : class => services.TryGetService(out service);
    public static I GetService<I>() where I : class => services.GetService<I>() ?? throw new ServiceNotFoundException<I>();

    public static void AddReplacementCallback<I>(Action<I> callback) where I : class
    {
        services.AddReplacementCallback<I>((serviceObj) => callback((I)serviceObj));
    }
}