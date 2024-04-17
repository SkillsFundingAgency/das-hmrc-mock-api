namespace SFA.DAS.HmrcMock.Web.Infrastructure;

internal class RestoreRawRequestPathMiddleware(RequestDelegate next, ILogger<RestoreRawRequestPathMiddleware> logger)
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
                // var newQueryString = context.Request.QueryString;
                // var newUrl = newPath.Add(newQueryString);
                //
                // logger.LogInformation($"Redirecting to: {newUrl}");
                // context.Response.Redirect(newUrl);
                // return; // End the middleware pipeline
            }
        }

        await _next(context);
    }

    private bool IsEpayePath(PathString requestPath)
    {
        return requestPath.Value.Contains("/api/apprenticeship-levy/epaye");
    }
}