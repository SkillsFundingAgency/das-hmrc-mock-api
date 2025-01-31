using System.Diagnostics.CodeAnalysis;
using SFA.DAS.HmrcMock.Domain.Interfaces;

namespace SFA.DAS.HmrcMock.Web.Services;

[ExcludeFromCodeCoverage]
public class DateTimeService : IDateTimeService
{
    public DateTime GetDateTime() => DateTime.UtcNow;
}
