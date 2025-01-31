using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Controllers;
using SFA.DAS.HmrcMock.Web.Models;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers;

public class GrantScopeControllerTests
{
    [Test, MoqAutoData]
    public void Get_Show_Redirect_With_AuthId(
        [Greedy] GrantScopeController controller)
    {
        // Arrange
        const string authId = "c29tZS1hdXRoLWlk";
        
        // Act
        var actual = controller.Show(authId);

        // Assert
        actual.Should().NotBeNull();
        var redirectResult = actual.Should().BeAssignableTo<RedirectResult>();
        redirectResult.Subject.Url.Should()
            .Be("/gg/sign-in?continue=/oauth/grantscope?auth_id=c29tZS1hdXRoLWlk&origin=oauth-frontend");
    }
}
