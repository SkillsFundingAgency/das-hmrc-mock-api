
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Logging.ApplicationInsights;
using SFA.DAS.HmrcMock.Domain.Configuration;
using SFA.DAS.HmrcMock.Web.AppStart;
using SFA.DAS.HmrcMock.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var isIntegrationTest = builder.Environment.EnvironmentName.Equals("IntegrationTest", StringComparison.CurrentCultureIgnoreCase);
var rootConfiguration = builder.Configuration.LoadConfiguration(isIntegrationTest);

var hmrcMockConfiguration = rootConfiguration.GetSection(nameof(HmrcMockConfiguration)).Get<HmrcMockConfiguration>();
builder.Services.AddOptions();

builder.Services.AddConfigurationOptions(rootConfiguration);

builder.Services.Configure<IISServerOptions>(options => { options.AutomaticAuthentication = false; });

builder.Services.AddServiceRegistration();

builder.Services.AddHealthChecks();

builder.Services.AddSession();
builder.Services.AddHealthChecks();

builder.Services.AddDasDistributedMemoryCache(hmrcMockConfiguration!);

// Add the custom AllowAnonymousFilter to the services
builder.Services.AddScoped<AllowAnonymousFilter>();

builder.Services.Configure<RouteOptions>(options =>
{

}).AddMvc(options =>
{
    if (!isIntegrationTest)
    {
        options.Filters.Add(new IgnoreAntiforgeryTokenAttribute()); 
    }

    options.Filters.Add<AllowAnonymousFilter>();
}).AddControllersAsServices().AddNewtonsoftJson();

builder.Services.AddDataProtection(rootConfiguration);

builder.Services.AddLogging(builder =>
{
    builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
    builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);
});

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDasHealthChecks();

app.UseStaticFiles();
app.UseSession();
app.UseMiddleware<RestoreRawRequestPathMiddleware>();
app.UseRouting();

app.UseEndpoints(endpointBuilder =>
{
    // Map API controllers
    endpointBuilder.MapControllers();
    
    endpointBuilder.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=SignIn}");
});

app.Run();