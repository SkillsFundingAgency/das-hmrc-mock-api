namespace SFA.DAS.HmrcMock.Web.Models;

public record ViewModelBase
{
    private Dictionary<string, string?> ErrorDictionary { get; set; }

    protected ViewModelBase()
    {
        ErrorDictionary = new Dictionary<string, string?>();
    }

    protected string GetErrorMessage(string propertyName)
    {
        return (ErrorDictionary.Count != 0 && ErrorDictionary.TryGetValue(propertyName, out var value) ? value : "") ?? string.Empty;
    }
}