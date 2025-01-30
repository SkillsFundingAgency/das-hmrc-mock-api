using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using SFA.DAS.HmrcMock.Domain.Configuration;

namespace SFA.DAS.HmrcMock.Web.AppStart;

[ExcludeFromCodeCoverage]
public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HmrcMockConfiguration>(configuration.GetSection(nameof(HmrcMockConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<HmrcMockConfiguration>>()!.Value);
        
        // Configure options
        services.Configure<MongoDbOptions>(configuration.GetSection(nameof(MongoDbOptions)));
    }
}