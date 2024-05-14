using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Application.TokenHandlers;
using SFA.DAS.HmrcMock.Domain.Configuration;
using SFA.DAS.HmrcMock.Domain.Interfaces;
using SFA.DAS.HmrcMock.Web.Actions;
using SFA.DAS.HmrcMock.Web.Services;

namespace SFA.DAS.HmrcMock.Web.AppStart;

public static class AddServiceRegistrationExtension
{
    public static void AddServiceRegistration(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddScoped<GatewayUserAction>();
        services.AddScoped<IGatewayUserService, MongoGatewayUserService>();
        services.AddScoped<IScopeService, MongoScopeService>();
        services.AddScoped<IClientService, MongoClientService>();
        services.AddScoped<IAuthRequestService, MongoAuthRequestService>();
        services.AddScoped<IAuthCodeService, MongoAuthCodeService>();
        services.AddScoped<ICreateAccessTokenHandler, CreateAccessTokenHandler>();
        services.AddScoped<IAuthRecordService, MongoAuthRecordService>();
        services.AddScoped<IEmpRefService, MongoEmpRefService>();
        services.AddScoped<IFractionCalcService, MongoFractionCalcService>();
        services.AddScoped<IFractionService, MongoFractionService>();
        services.AddScoped<ILevyDeclarationService, MongoLevyDeclarationService>();
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var mongoDbOptions = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var mongoUrl = new MongoUrl(mongoDbOptions.ConnectionString);
            var mongoClient = new MongoClient(mongoUrl);
            return mongoClient.GetDatabase(mongoUrl.DatabaseName);
        });
    }
}