
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estehlaky.Infrastructure.Extensions
{
    public static class AddCachingExt
    {
        public static IServiceCollection AddAppCaching(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // TODO: Configure Redis when implemented.

            services.AddMemoryCache();

            // services.AddStackExchangeRedisCache(options =>
            // {
            //     options.Configuration =
            //         configuration.GetConnectionString("Redis");
            // });

            return services;
        }
    }
}
