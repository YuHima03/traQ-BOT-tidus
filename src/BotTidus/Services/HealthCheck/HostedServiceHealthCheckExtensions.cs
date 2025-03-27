using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace BotTidus.Services.HealthCheck
{
    internal static class HostedServiceHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddTypedHostedService<TImplementation>(this IHealthChecksBuilder builder)
            where TImplementation : class, IHostedService, IHealthCheck
        {
            return builder.Add(new HealthCheckRegistration(
                typeof(TImplementation).Name,
                sp => sp.GetServices<IHostedService>().OfType<TImplementation>().First(),
                HealthStatus.Unhealthy,
                []
            ));
        }
    }
}
