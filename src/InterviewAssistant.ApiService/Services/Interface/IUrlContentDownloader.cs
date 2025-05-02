namespace InterviewAssistant.ApiService.Services;

public interface IUrlContentDownloader
{
    /// <summary>
    /// URL에서 파일을 다운로드하고, 텍스트를 추출합니다.
    /// </summary>
    /// <param name="url">다운로드할 콘텐츠의 URL</param>
    /// <returns>추출된 텍스트 내용</returns>
    Task<string> DownloadAndExtractTextAsync(string url);
}
