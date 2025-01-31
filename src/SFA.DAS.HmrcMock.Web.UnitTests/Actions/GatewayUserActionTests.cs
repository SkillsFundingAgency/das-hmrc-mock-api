using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Actions;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Actions;

[TestFixture]
public class GatewayUserActionTests
{
    private Mock<IGatewayUserService> _gatewayUserServiceMock;
    private GatewayUserAction _filter;
    private ActionExecutingContext _context;
    private ActionExecutionDelegate _next;

    [SetUp]
    public void SetUp()
    {
        _gatewayUserServiceMock = new Mock<IGatewayUserService>();
        _filter = new GatewayUserAction(_gatewayUserServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
        };

        _context = new ActionExecutingContext(
            actionContext,
            new System.Collections.Generic.List<IFilterMetadata>(),
            new System.Collections.Generic.Dictionary<string, object>(),
            new object());

        _next = () => Task.FromResult(new ActionExecutedContext(actionContext, new System.Collections.Generic.List<IFilterMetadata>(), new object()));
    }

    [Test]
    public async Task Should_AllowAccess_ToSignInAction_WithoutAuthentication()
    {
        // Arrange
        _context.HttpContext.Request.Path = "/gg/sign-in";
        
        // Act
        await _filter.OnActionExecutionAsync(_context, _next);
        
        // Assert
        _context.Result.Should().BeNull();
    }

    [Test]
    public async Task Should_RedirectToSignIn_WhenUserNotAuthenticated()
    {
        // Arrange
        _context.HttpContext.Request.Path = "/secure-area";
        
        // Act
        await _filter.OnActionExecutionAsync(_context, _next);
        
        // Assert
        _context.Result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("SignIn");
    }

    [Test]
    public async Task Should_RedirectToSignIn_WhenUserNotFound()
    {
        // Arrange
        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c.TryGetValue("validatedUser", out It.Ref<string>.IsAny))
            .Returns((string key, out string value) =>
            {
                value = "non-existent-user";
                return true;
            });
        
        _context.HttpContext.Request.Cookies = cookieCollection.Object;
        _context.HttpContext.Request.Path = "/secure-area";
        _gatewayUserServiceMock.Setup(s => s.GetByGatewayIdAsync(It.IsAny<string>())).ReturnsAsync((GatewayUserResponse)null);
        
        // Act
        await _filter.OnActionExecutionAsync(_context, _next);
        
        // Assert
        _context.Result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("SignIn");
    }

    [Test]
    public async Task Should_AllowAccess_WhenUserIsAuthenticated()
    {
        // Arrange
        var user = new GatewayUserResponse { GatewayID = "valid-user" };
        _context.HttpContext.Request.Path = "/secure-area";
        
        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c.TryGetValue("validatedUser", out It.Ref<string>.IsAny))
            .Returns((string _, out string value) =>
            {
                value = "valid-user";
                return true;
            });
        
        _context.HttpContext.Request.Cookies = cookieCollection.Object;
        _gatewayUserServiceMock.Setup(s => s.GetByGatewayIdAsync("valid-user")).ReturnsAsync(user);
        
        // Act
        await _filter.OnActionExecutionAsync(_context, _next);
        
        // Assert
        _context.HttpContext.Items.ContainsKey("GatewayUser").Should().BeTrue();
        _context.HttpContext.Items["GatewayUser"].Should().Be(user);
    }
}
