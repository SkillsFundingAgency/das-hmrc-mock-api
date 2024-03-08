using System.ComponentModel.DataAnnotations;

namespace SFA.DAS.HmrcMock.Web.Models;

public record SigninViewModel : ViewModelBase
{
    [Required(ErrorMessage = "Enter your User ID")]
    [MinLength(1)]
    public string? UserId { get; init; }
    [Required(ErrorMessage = "Enter your password")]
    [MinLength(1)]
    public string? Password { get; init; }
    
    public string? Continue { get; init; }
    public string? Origin { get; init; }
    
    public string UserIdError => GetErrorMessage(nameof(UserId));
    public string PasswordError => GetErrorMessage(nameof(Password));
}