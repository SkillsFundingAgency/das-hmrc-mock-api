using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Domain.Interfaces;
using SFA.DAS.HmrcMock.Web.Services;

namespace SFA.DAS.HmrcMock.Web.AppStart;

public static class AddServiceRegistrationExtension
{
    public static void AddServiceRegistration(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IGatewayUserService, GatewayUserService>();
    }
}