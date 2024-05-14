using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SFA.DAS.HmrcMock.Web.AppStart;

public static class HealthCheckStartupExtensions
{
    public static IApplicationBuilder UseDasHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/ping", new HealthCheckOptions
        {
            Predicate = (_) => false,
            ResponseWriter = (context, report) =>
            {
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("");
            }
        });

        return app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (httpContext, report) => httpContext.Response.WriteJsonAsync(new
            {
                report.Status,
                report.TotalDuration,
                Results = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new
                    {
                        e.Value.Status,
                        e.Value.Duration,
                        e.Value.Description,
                        e.Value.Data
                    })
            })
        });
    }
}