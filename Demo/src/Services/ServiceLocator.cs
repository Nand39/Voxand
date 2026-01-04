using Sparkvox.src.ExecutionControl;
using Voxand.Helpers.Exceptions;

namespace Sparkvox.src.Services;
public static class ServiceLocator
{
    static PrimaryManager.ServiceRegistry services = null!;
    internal static void Initialize(PrimaryManager.ServiceRegistry services)
    {
        if (ServiceLocator.services is not null)
            throw new InvalidOperationException($"{nameof(ServiceLocator)} can only be initialized once.");

        ServiceLocator.services = services ?? throw new ArgumentNullException($"{nameof(services)} cannot be null.");
    }
    public static bool TryGetService<I>(out I service) where I : class => services.TryGetService(out service);
    public static I GetService<I>() where I : class => services.GetService<I>() ?? throw new ServiceNotFoundException<I>();

    public static void AddReplacementCallback<I>(Action<I> callback) where I : class
    {
        services.AddReplacementCallback<I>((serviceObj) => callback((I)serviceObj));
    }
}