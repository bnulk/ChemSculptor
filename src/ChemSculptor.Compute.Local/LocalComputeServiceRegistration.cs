using ChemSculptor.Compute;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Compute.Local;

/// <summary>本机计算执行后端注册。</summary>
public static class LocalComputeServiceRegistration
{
    /// <summary>注册本机执行后端。</summary>
    public static IServiceCollection AddLocalComputeServices(
        IServiceCollection services)
    {
        LocalProcessBackendOptions options =
            LocalProcessBackendOptions.CreateDefault();

        ServiceCollectionServiceExtensions.AddSingleton<LocalProcessBackendOptions>(
            services,
            options);
        ServiceCollectionServiceExtensions.AddSingleton<
            IComputeBackend,
            LocalProcessBackend>(services);

        return services;
    }
}
