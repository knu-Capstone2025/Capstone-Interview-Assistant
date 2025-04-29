using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InterviewAssistant.ApiService.Services;

public class GoogleDriveDownloader(HttpClient httpClient)
{
    /// <summary>
    /// 구글 드라이브 공유 링크에서 파일을 다운로드하고 텍스트를 추출합니다.
    /// </summary>
    /// <param name="fileUrl">구글 드라이브 공유 링크</param>
    /// <returns>추출된 텍스트 내용</returns>
    /// <exception cref="ArgumentException">구글 드라이브 링크가 아닌 경우 발생</exception>
    public async Task<string> DownloadAndExtractTextAsync(string fileUrl)
    {
        try
        {
            if (!fileUrl.Contains("drive.google.com"))
            {
                throw new ArgumentException("구글 드라이브 링크만 지원합니다. 제공된 URL은 구글 드라이브 링크가 아닙니다.", nameof(fileUrl));
            }

            string directDownloadUrl = ConvertToDirectDownloadUrl(fileUrl);

            if (directDownloadUrl == fileUrl)
            {
                throw new ArgumentException("유효한 구글 드라이브 공유 링크 형식이 아닙니다.", nameof(fileUrl));
            }

            var response = await httpClient.GetAsync(directDownloadUrl);

            var contentType = response.Content.Headers.ContentType?.MediaType;

            if (contentType != null && contentType.StartsWith("text/html"))
            {
                throw new UnauthorizedAccessException("이 파일은 비공개이거나 접근 권한이 필요합니다. 파일 소유자에게 공유 설정을 요청하세요.");
            }

            byte[] fileData = await httpClient.GetByteArrayAsync(directDownloadUrl);

            string textContent = Encoding.UTF8.GetString(fileData);

            return textContent;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"파일 다운로드 또는 텍스트 추출 중 오류 발생: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"내부 예외: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// 구글 드라이브 공유 링크를 직접 다운로드 URL로 변환합니다.
    /// </summary>
    private string ConvertToDirectDownloadUrl(string shareUrl)
    {
        if (shareUrl.Contains("drive.google.com"))
        {
            string? fileId = ExtractGoogleDriveFileId(shareUrl);

            if (!string.IsNullOrEmpty(fileId))
            {
                return $"https://drive.google.com/uc?export=download&id={fileId}";
            }
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
