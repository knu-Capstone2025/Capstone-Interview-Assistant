#pragma warning disable CS1998

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using InterviewAssistant.ApiService.Services;

using NSubstitute;

using NUnit.Framework;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Services;

[TestFixture]
public class GoogleDriveDownloaderTests
{
    private MockHttpMessageHandler _mockHandler;
    private HttpClient _httpClient;
    private GoogleDriveDownloader _googleDriveDownloader;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new MockHttpMessageHandler();
        
        _httpClient = new HttpClient(_mockHandler);
        
        _httpClient.BaseAddress = new Uri("https://example.com/");
        
        _googleDriveDownloader = new GoogleDriveDownloader(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHandler.Dispose();
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithValidGoogleDriveUrl_ShouldReturnContent()
    {
        // Arrange
        var validUrl = "https://drive.google.com/file/d/1234567890abcdef/view?usp=sharing";
        var expectedContent = "This is the file content";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedContent);

        // MockHandler 설정
        _mockHandler.ResponseToReturn = expectedBytes;

        // Act
        var result = await _googleDriveDownloader.DownloadAndExtractTextAsync(validUrl);

        // Assert
        result.ShouldBe(expectedContent);
    }

    [Test]
    public void DownloadAndExtractTextAsync_WithNonGoogleDriveUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidUrl = "https://example.com/file.txt";

        // Act & Assert
        Should.Throw<ArgumentException>(async () =>
        {
            await _googleDriveDownloader.DownloadAndExtractTextAsync(invalidUrl);
        }).ParamName.ShouldBe("fileUrl");
    }

    [Test]
    public void DownloadAndExtractTextAsync_WithInvalidGoogleDriveFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidFormatUrl = "https://drive.google.com/invalid/format";

        // Act & Assert
        Should.Throw<ArgumentException>(async () =>
        {
            await _googleDriveDownloader.DownloadAndExtractTextAsync(invalidFormatUrl);
        }).ParamName.ShouldBe("fileUrl");
    }

    [Test]
    public void DownloadAndExtractTextAsync_WithHttpClientError_ShouldPropagateException()
    {
        // Arrange
        var validUrl = "https://drive.google.com/file/d/1234567890abcdef/view";
        var expectedException = new HttpRequestException("Network error");

        // MockHandler 설정 - 예외 발생
        _mockHandler.ExceptionToThrow = expectedException;

        // Act & Assert
        Should.Throw<Exception>(async () =>
        {
            await _googleDriveDownloader.DownloadAndExtractTextAsync(validUrl);
        });
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithOpenIdFormat_ShouldReturnContent()
    {
        // Arrange
        var openIdUrl = "https://drive.google.com/open?id=1234567890abcdef";
        var expectedContent = "This is the content from open id format";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedContent);

        // MockHandler 설정
        _mockHandler.ResponseToReturn = expectedBytes;

        // Act
        var result = await _googleDriveDownloader.DownloadAndExtractTextAsync(openIdUrl);

        // Assert
        result.ShouldBe(expectedContent);
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithIdQueryParameter_ShouldReturnContent()
    {
        // Arrange
        var idQueryUrl = "https://drive.google.com/something?id=1234567890abcdef&extra=param";
        var expectedContent = "This is the content from id query parameter";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedContent);

        // MockHandler 설정
        _mockHandler.ResponseToReturn = expectedBytes;

        // Act
        var result = await _googleDriveDownloader.DownloadAndExtractTextAsync(idQueryUrl);

        // Assert
        result.ShouldBe(expectedContent);
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithLargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var validUrl = "https://drive.google.com/file/d/1234567890abcdef/view";
        var largeContent = new string('A', 10_000); // 10KB 문자열
        var expectedBytes = Encoding.UTF8.GetBytes(largeContent);

        // MockHandler 설정
        _mockHandler.ResponseToReturn = expectedBytes;

        // Act
        var result = await _googleDriveDownloader.DownloadAndExtractTextAsync(validUrl);

        // Assert
        result.Length.ShouldBe(largeContent.Length);
        result.ShouldBe(largeContent);
    }

    [Test]
    public async Task DownloadAndExtractTextAsync_WithUTF8Characters_ShouldPreserveEncoding()
    {
        // Arrange
        var validUrl = "https://drive.google.com/file/d/1234567890abcdef/view";
        var contentWithUTF8 = "UTF8 문자열 테스트 with special chars: 한글, ñ, é, ß, 😊";
        var expectedBytes = Encoding.UTF8.GetBytes(contentWithUTF8);

        // MockHandler 설정
        _mockHandler.ResponseToReturn = expectedBytes;

        // Act
        var result = await _googleDriveDownloader.DownloadAndExtractTextAsync(validUrl);

        // Assert
        result.ShouldBe(contentWithUTF8);
    }
}

/// <summary>
/// 테스트용 커스텀 HttpMessageHandler
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler, IDisposable
{
    public byte[] ResponseToReturn { get; set; }
    public Exception ExceptionToThrow { get; set; }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(ResponseToReturn ?? Array.Empty<byte>())
        };
    }

}
