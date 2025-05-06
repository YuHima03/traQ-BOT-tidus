using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotTidus.Services.HealthCheck
{
    internal static class MySqlHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddMySqlWithDbContext<TDbContext>(this IHealthChecksBuilder builder)
            where TDbContext : DbContext
        {
            return builder.Add(new HealthCheckRegistration(
                typeof(TDbContext).Name,
                static sp => new MySqlHealthCheck<TDbContext>(sp.GetRequiredService<IDbContextFactory<TDbContext>>()),
                HealthStatus.Unhealthy,
                []
            ));
        }
    }

    file sealed class MySqlHealthCheck<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory) : IHealthCheck
        where TDbContext : DbContext
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                await dbContext.Database.ExecuteSqlAsync($"SELECT 1;", cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("MySQL server is not healthy.", e);
            }
        }
    }
}
