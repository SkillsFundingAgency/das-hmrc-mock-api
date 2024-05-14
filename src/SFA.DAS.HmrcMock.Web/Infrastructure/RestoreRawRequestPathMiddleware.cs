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
                // Extracting the original path from the unencoded URL
                var originalPath = new PathString(unencodedUrl.Substring(context.Request.PathBase.ToUriComponent().Length));

                // Extracting the query string parameters from the original path
                var queryString = string.Empty;
                var queryIndex = originalPath.Value.IndexOf('?');
                if (queryIndex >= 0)
                {
                    queryString = originalPath.Value.Substring(queryIndex);
                    originalPath = originalPath.Value.Substring(0, queryIndex);
                }

                // Setting the new path with the original path as a query string parameter
                context.Request.Path = originalPath;

                if (!string.IsNullOrEmpty(queryString))
                {
                    context.Request.QueryString = new QueryString(queryString);
                }
            }
        }

        await _next(context);
    }

    private static bool IsEpayePath(PathString requestPath)
    {
        return requestPath.Value!.Contains("/api/apprenticeship-levy/epaye");
    }
}