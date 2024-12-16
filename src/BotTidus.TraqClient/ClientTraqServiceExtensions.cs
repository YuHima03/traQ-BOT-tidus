using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotTidus.TraqClient
{
    public static class ClientTraqServiceExtensions
    {
        public static IServiceCollection AddTraqClient<TService>(this IServiceCollection collection, Action<IClientTraqBuilder> configurator)
            where TService : class, IClientTraqService
        {
            ClientTraqBuilder builder = new();
            configurator.Invoke(builder);
            collection.TryAddSingleton<IClientTraqService>(builder.Build);
            return collection;
        }
    }
}
