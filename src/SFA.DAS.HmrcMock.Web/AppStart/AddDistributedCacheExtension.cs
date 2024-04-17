using System.Diagnostics.CodeAnalysis;
using SFA.DAS.HmrcMock.Domain.Configuration;

namespace SFA.DAS.HmrcMock.Web.AppStart;

[ExcludeFromCodeCoverage]
public static class AddDistributedCacheExtension
{
    public static IServiceCollection AddDasDistributedMemoryCache(this IServiceCollection services,
        HmrcMockConfiguration configuration)
    {
#if DEBUG
        services.AddDistributedMemoryCache();
#else
            services.AddStackExchangeRedisCache(o => o.Configuration = configuration.RedisConnectionString);
#endif

        return services;
    }
}