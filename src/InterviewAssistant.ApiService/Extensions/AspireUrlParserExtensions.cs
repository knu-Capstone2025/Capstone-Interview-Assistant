namespace InterviewAssistant.ApiService.Extensions;

/// <summary>
/// Provides an extension method to resolve Aspire-style service URIs to actual HTTP or HTTPS endpoints.
/// </summary>
public static class AspireUrlParserExtensions
{
    /// <summary>
    /// Resolves an Aspire-style URI (e.g., 'https+http://service') to a real HTTP or HTTPS URI from configuration.
    /// </summary>
    /// <param name="uri">The service URI to resolve.</param>
    /// <param name="config">The configuration containing service endpoint mappings.</param>
    /// <returns>The resolved absolute URI.</returns>
    public static Uri Resolve(this Uri uri, IConfiguration config)
    {
        var absoluteUrl = uri.ToString();
        if (absoluteUrl.StartsWith("http://") || absoluteUrl.StartsWith("https://"))
        {
            return uri;
        }
        if (absoluteUrl.StartsWith("https+http://"))
        {
            var appname = absoluteUrl.Substring("https+http://".Length).Split('/')[0];
            var https = config[$"services:{appname}:https:0"]!;
            var http = config[$"services:{appname}:http:0"]!;

            return string.IsNullOrWhiteSpace(https)
                   ? new Uri(http + "/sse")
                   : new Uri(https + "/sse");
        }

        throw new InvalidOperationException($"Invalid URL format: {absoluteUrl}. Expected format: 'https+http://appname' or 'http://appname'.");
    }
}
