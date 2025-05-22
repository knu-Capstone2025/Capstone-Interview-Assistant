using System.Text;
using System.Text.RegularExpressions;

namespace InterviewAssistant.ApiService.Services;

public class UrlContentDownloader(HttpClient httpClient) : IUrlContentDownloader
{

    // 정규식을 정적 필드로 컴파일하여 저장
    private static readonly Regex GoogleDriveIdPattern = new Regex(
        @"https?://drive\.google\.com/.*(?:file/d/|id=)([^/&?#]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<string> DownloadTextAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        // 단축 URL이면 실제 URL로 리다이렉션 처리
        string actualUrl = await FollowRedirectsAsync(url);

        // 구글 드라이브 링크인 경우 직접 다운로드 URL로 변환
        if (actualUrl.Contains("drive.google.com"))
        {
            actualUrl = ConvertToDirectDownloadUrl(actualUrl);
        }

        // HTTP 요청 생성 및 응답 확인
        var response = await httpClient.GetAsync(actualUrl);
        response.EnsureSuccessStatusCode();

        // 컨텐츠 타입 확인
        var contentType = response.Content.Headers.ContentType?.ToString();

        // HTML인 경우 웹페이지일 수 있음
        if (contentType != null && contentType.Contains("text/html"))
        {
            string content = await response.Content.ReadAsStringAsync();

            // 구글 드라이브인 경우 접근 권한 확인
            if (actualUrl.Contains("drive.google.com") && (content.Contains("Sign in") || content.Contains("로그인")))
            {
                throw new UnauthorizedAccessException("이 파일은 비공개이거나 접근 권한이 필요합니다.");
            }

            // 일반 웹페이지인 경우 HTML 내용 반환
            return content;
        }

        return await ExtractTextFromResponseAsync(response);
    }

    /// <summary>
    /// 바이너리 파일 또는 텍스트 파일에서 텍스트를 추출합니다.
    /// </summary>
    private async Task<string> ExtractTextFromResponseAsync(HttpResponseMessage response)
    {
        byte[] fileData = await response.Content.ReadAsByteArrayAsync();
        return Encoding.UTF8.GetString(fileData);
    }

    /// <summary>
    /// 단축 URL이나 리다이렉션이 있는 URL을 따라가 최종 URL을 반환합니다.
    /// </summary>
    private async Task<string> FollowRedirectsAsync(string url)
    {

        // 기존 HttpClient의 DefaultRequestHeaders 저장
        var originalHeaders = new Dictionary<string, IEnumerable<string>>();
        foreach (var header in httpClient.DefaultRequestHeaders)
        {
            originalHeaders[header.Key] = header.Value;
        }

        // 리다이렉션을 수동으로 처리하기 위한 HEAD 요청
        var request = new HttpRequestMessage(HttpMethod.Head, url);

        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // 리다이렉션 응답인 경우(3xx) Location 헤더에서 대상 URL 가져오기
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400 &&
                response.Headers.Location != null)
            {
                var redirectUrl = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location.ToString()
                    : new Uri(new Uri(url), response.Headers.Location).ToString();

                // 재귀적으로 리다이렉션을 따라감
                if (url != redirectUrl)
                {
                    return await FollowRedirectsAsync(redirectUrl);
                }
            }
            return url;
        }
        catch
        {
            return url;
        }
    }

    /// <summary>
    /// 구글 드라이브 공유 링크를 직접 다운로드 URL로 변환합니다.
    /// </summary>
    private string ConvertToDirectDownloadUrl(string shareUrl)
    {
        string? fileId = ExtractGoogleDriveFileId(shareUrl);

        if (string.IsNullOrWhiteSpace(fileId) == false)
        {
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }

        return shareUrl;
    }

    /// <summary>
    /// 구글 드라이브 공유 링크에서 파일 ID를 추출합니다.
    /// </summary>
    private string? ExtractGoogleDriveFileId(string googleDriveUrl)
    {
        var match = GoogleDriveIdPattern.Match(googleDriveUrl);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}
