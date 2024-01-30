using Microsoft.Extensions.Options;
using SFA.DAS.HmrcMock.Domain.Configuration;

namespace SFA.DAS.HmrcMock.Web.AppStart;

public static class AddConfigurationOptionsExtension
{
    public static void AddConfigurationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HmrcMockConfiguration>(configuration.GetSection(nameof(HmrcMockConfiguration)));
        services.AddSingleton(cfg => cfg.GetService<IOptions<HmrcMockConfiguration>>()!.Value);
    }
}