using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Web.AppStart;

var builder = WebApplication.CreateBuilder(args);

var isIntegrationTest = builder.Environment.EnvironmentName.Equals("IntegrationTest", StringComparison.CurrentCultureIgnoreCase);
var rootConfiguration = builder.Configuration.LoadConfiguration(isIntegrationTest);

builder.Services.AddOptions();
builder.Services.AddConfigurationOptions(rootConfiguration);

builder.Services.AddLogging();
builder.Services.Configure<IISServerOptions>(options => { options.AutomaticAuthentication = false; });

builder.Services.AddServiceRegistration();

builder.Services.AddHealthChecks();

builder.Services.AddSession();

builder.Services.Configure<RouteOptions>(options =>
{

}).AddMvc(options =>
{
    if (!isIntegrationTest)
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    }

    // options.Filters.Add<GatewayUserAction>();
}).AddControllersAsServices(); ;

builder.Services.AddDataProtection(rootConfiguration);

builder.Services.AddApplicationInsightsTelemetry();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

}

app.UseHealthChecks("/ping");

app.UseRouting();

app.UseStaticFiles();

app.UseSession();

app.UseEndpoints(endpointBuilder =>
{
    // Map API controllers
    endpointBuilder.MapControllers();
    
    endpointBuilder.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=SignIn}");
});

app.Run();
