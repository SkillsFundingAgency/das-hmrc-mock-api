namespace SFA.DAS.HmrcMock.Web.Infrastructure;

internal class RestoreRawRequestPathMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private const string UnEncodedUrlPathHeaderName = "X-Waws-Unencoded-Url";

    public async Task Invoke(HttpContext context, ILogger<RestoreRawRequestPathMiddleware> logger)
    {
        var hasUnencodedUrl = context.Request.Headers.TryGetValue(UnEncodedUrlPathHeaderName, out var unencodedUrlValue);
        if (IsEpayePath(context.Request.Path) && hasUnencodedUrl && unencodedUrlValue.Count > 0)
        {
            var unencodedUrl = unencodedUrlValue.First();
            if (unencodedUrlValue != context.Request.Path)
            {
                // Redirect to the new path
                var newPath = new PathString(unencodedUrl);
                context.Request.Path = newPath;
            }
        }

        await _next(context);
    }

    private static bool IsEpayePath(PathString requestPath)
    {
        return requestPath.Value!.Contains("/api/apprenticeship-levy/epaye");
    }
}