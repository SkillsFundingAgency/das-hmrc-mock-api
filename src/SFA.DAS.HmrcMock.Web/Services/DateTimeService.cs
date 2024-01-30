using SFA.DAS.HmrcMock.Domain.Interfaces;

namespace SFA.DAS.HmrcMock.Web.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime GetDateTime() => DateTime.UtcNow;
}
