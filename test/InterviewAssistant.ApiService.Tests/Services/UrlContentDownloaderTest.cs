#pragma warning disable CS1998

using System;
using System.Threading;
using System.Threading.Tasks;

using InterviewAssistant.ApiService.Services;
using InterviewAssistant.ApiService.Tests.Common;

using NUnit.Framework;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class UrlContentDownloaderTests
{
    private UrlContentDownloader? _downloader;
    private MockHttpMessageHandler? _mockHttpMessageHandler;
    private HttpClient? _httpClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // 모든 테스트에서 공유할 MockHttpMessageHandler 생성
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpMessageHandler);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // 리소스 정리
        _httpClient?.Dispose();
        _mockHttpMessageHandler?.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        // 각 테스트마다 UrlContentDownloader 초기화
        _downloader = new UrlContentDownloader(_httpClient!);
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithEmptyUrl_ShouldThrowArgumentException()
    {
        // Arrange
        string url = "   ";

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(async () =>
            await _downloader!.DownloadTextAsync(url));

        exception.ParamName.ShouldBe("url");
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithValidTextUrl_ShouldReturnContent()
    {
        // Arrange
        var url = "http://example.com/file.txt";
        var expectedContent = "This is a test content";

        _mockHttpMessageHandler!.When(url)
            .Respond("text/plain", expectedContent);

        // Act
        var result = await _downloader!.DownloadTextAsync(url);

        // Assert
        result.ShouldBe(expectedContent);
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithGoogleDriveUrl_ShouldConvertAndDownload()
    {
        // Arrange
        var originalUrl = "https://drive.google.com/file/d/1234567890/view";
        var expectedDirectUrl = "https://drive.google.com/uc?export=download&id=1234567890";
        var expectedContent = "Google Drive file content";

        // 다운로드 URL에 대한 응답 설정
        _mockHttpMessageHandler!.When(expectedDirectUrl)
            .Respond("text/plain", expectedContent);

        // Act
        var result = await _downloader!.DownloadTextAsync(originalUrl);

        // Assert
        result.ShouldBe(expectedContent);
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithPrivateGoogleDriveFile_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var url = "https://drive.google.com/file/d/1234567890/view";
        var expectedDirectUrl = "https://drive.google.com/uc?export=download&id=1234567890";
        var googleDriveResponse = "<html><body>Sign in</body></html>";

        // 다운로드 URL에 대한 응답 설정
        _mockHttpMessageHandler!.When(expectedDirectUrl)
            .Respond("text/html", googleDriveResponse);

        // Act & Assert
        Should.Throw<UnauthorizedAccessException>(async () =>
            await _downloader!.DownloadTextAsync(url));
    }
}

