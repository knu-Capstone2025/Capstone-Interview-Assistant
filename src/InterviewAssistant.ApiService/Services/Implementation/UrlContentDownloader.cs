using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InterviewAssistant.ApiService.Services;

public class UrlContentDownloader(HttpClient httpClient, ILogger<UrlContentDownloader> logger) : IUrlContentDownloader
{
    public async Task<string> DownloadAndExtractTextAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL이 비어있거나 null입니다.", nameof(url));
        }

        try
        {
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

            // 바이너리 파일 또는 텍스트 파일 다운로드
            byte[] fileData = await response.Content.ReadAsByteArrayAsync();
            
            // 텍스트로 변환 시도
            try
            {
                return Encoding.UTF8.GetString(fileData);
            }
            catch (Exception)
            {
                try 
                {
                    // UTF-8로 변환 실패 시 다른 인코딩 시도
                    return Encoding.GetEncoding(1252).GetString(fileData);
                }
                catch (Exception ex)
                {
                    logger.LogError($"텍스트 변환 실패: {ex.Message}");
                    throw new InvalidOperationException("다운로드한 파일을 텍스트로 변환할 수 없습니다. 텍스트 파일이 아닐 수 있습니다.");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"HTTP 요청 오류: {ex.Message}");
            throw new Exception($"URL에서 콘텐츠를 다운로드하는 중 오류가 발생했습니다: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is UnauthorizedAccessException || ex is InvalidOperationException))
        {
            logger.LogError($"예상치 못한 오류: {ex.Message}");
            throw new Exception($"콘텐츠 다운로드 중 예상치 못한 오류가 발생했습니다: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 단축 URL이나 리다이렉션이 있는 URL을 따라가 최종 URL을 반환합니다.
    /// </summary>
    private async Task<string> FollowRedirectsAsync(string url)
    {
        try
        {
            // 최초 요청 시 리다이렉션을 자동으로 따라가지 않도록 설정
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            
            using var client = new HttpClient(handler);
            
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request);
            
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
        catch (Exception)
        {
            // 리다이렉션 처리 중 오류 발생 시 원래 URL 반환
            return url;
        }
    }

    /// <summary>
    /// 구글 드라이브 공유 링크를 직접 다운로드 URL로 변환합니다.
    /// </summary>
    private string ConvertToDirectDownloadUrl(string shareUrl)
    {
        string? fileId = ExtractGoogleDriveFileId(shareUrl);

        if (!string.IsNullOrEmpty(fileId))
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
        var patterns = new[]
        {
            @"https://drive\.google\.com/file/d/(.*?)(/|$)",
            @"https://drive\.google\.com/open\?id=(.*?)($|&)",
            @"id=(.*?)($|&)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(googleDriveUrl, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }
}
