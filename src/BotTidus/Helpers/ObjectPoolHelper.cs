using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace BotTidus.Helpers
{
    internal static class ObjectPoolHelper
    {
        public static IServiceCollection AddObjectPool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this IServiceCollection services)
            where T : class
        {
            services.TryAddSingleton(sp =>
            {
                var provider = sp.GetRequiredService<ObjectPoolProvider>();
                return provider.Create(new PooledObjectPolicySlim<T>());
            });
            return services;
        }
    }

    file sealed class PooledObjectPolicySlim<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : IPooledObjectPolicy<T>
        where T : class
    {
        static readonly Func<T> DefaultConstructor = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();

        public T Create() => DefaultConstructor.Invoke();

        public bool Return(T obj) => true;
    }
}
