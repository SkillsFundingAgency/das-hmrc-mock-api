using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Actions;

public class GatewayUserAction(IGatewayUserService gatewayUsers) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Path.Equals("/gg/sign-in", StringComparison.OrdinalIgnoreCase))
        {
            await next(); // Allow access to the SignIn action without authentication
            return;
        }
        
        if (!context.HttpContext.Request.Cookies.TryGetValue("validatedUser", out var userId))
        {
            context.Result = new RedirectToActionResult("SignIn", "Home",
                new { continueUrl = context.HttpContext.Request.Path });
            return;
        }

        var user = await gatewayUsers.GetByGatewayIdAsync(userId);
        if (user == null)
        {
            context.Result = new RedirectToActionResult("SignIn", "Home",
                new { continueUrl = context.HttpContext.Request.Path });
            return;
        }

        context.HttpContext.Items["GatewayUser"] = user;

        await next();
    }
}