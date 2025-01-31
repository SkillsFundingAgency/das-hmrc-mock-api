using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFA.DAS.HmrcMock.Web.AppStart;

[ExcludeFromCodeCoverage]
public static class HttpResponseExtensions
{
    public static Task WriteJsonAsync(this HttpResponse httpResponse, object body)
    {
        httpResponse.ContentType = "application/json";

        return httpResponse.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        }));
    }
}