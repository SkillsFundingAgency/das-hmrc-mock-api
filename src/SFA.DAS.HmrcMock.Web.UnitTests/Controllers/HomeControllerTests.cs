using AutoFixture.NUnit3;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Web.Controllers;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers;
public class HomeControllerTests
{
    [Test, MoqAutoData]
    public void Then_The_View_Is_Returned(
        [Frozen] Mock<IMediator> mediator,
        [Greedy] HomeController controller)
    {
        // Act
        var actual = controller.SignIn();

        // Assert
        using (new AssertionScope())
        {
            actual.Should().NotBeNull();
            actual.Should().BeAssignableTo<ViewResult>();
        }
    }
}
