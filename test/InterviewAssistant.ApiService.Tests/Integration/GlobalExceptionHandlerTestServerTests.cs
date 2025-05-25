using System.Net;
using System.Text.Json;
using System.Linq;

using InterviewAssistant.ApiService.Data;
using InterviewAssistant.ApiService.Endpoints;
using InterviewAssistant.ApiService.Middlewares;
using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using OpenAI;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Integration;

[TestFixture]
public class GlobalExceptionHandlerTestServerTests
{
    private TestServer _server;
    private HttpClient _client;
    private IKernelService _mockKernelService;
    private IInterviewRepository _mockRepository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _mockKernelService = Substitute.For<IKernelService>();
        _mockRepository = Substitute.For<IInterviewRepository>();

        var hostBuilder = CreateHostBuilder();
        _server = new TestServer(hostBuilder);
        _client = _server.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _mockKernelService.ClearReceivedCalls();
        _mockRepository.ClearReceivedCalls();
    }

    [Test]
    public async Task ChatCompletion_WhenKernelServiceThrowsException_ShouldReturnGlobalExceptionResponse()
    {
        // Arrange
        var validResumeId = Guid.NewGuid();
        var validJobId = Guid.NewGuid();
        
        var chatRequest = new ChatRequest
        {
            ResumeId = validResumeId,
            JobDescriptionId = validJobId,
            Messages = new List<ChatMessage>
            {
                new() { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        _mockRepository.GetResumeByIdAsync(validResumeId)
            .Returns(new Models.ResumeEntry { Id = validResumeId, Content = "테스트 이력서" });
        _mockRepository.GetJobByIdAsync(validJobId)
            .Returns(new Models.JobDescriptionEntry { Id = validJobId, Content = "테스트 채용공고" });

        _mockKernelService.InvokeInterviewAgentAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<IEnumerable<Microsoft.SemanticKernel.ChatMessageContent>>())
            .Throws(new InvalidOperationException("커널 서비스 오류 발생"));

        var json = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/chat/complete", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        errorResponse.GetProperty("error").GetString().ShouldBe("서버 오류가 발생했습니다.");
        errorResponse.GetProperty("message").GetString().ShouldBe("커널 서비스 오류 발생");
        errorResponse.TryGetProperty("timestamp", out _).ShouldBeTrue();
    }

    [Test]
    public async Task ChatCompletion_WhenRepositoryThrowsException_ShouldReturnGlobalExceptionResponse()
    {
        // Arrange
        var validResumeId = Guid.NewGuid();
        var validJobId = Guid.NewGuid();
        
        var chatRequest = new ChatRequest
        {
            ResumeId = validResumeId,
            JobDescriptionId = validJobId,
            Messages = new List<ChatMessage>
            {
                new() { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        _mockRepository.GetResumeByIdAsync(validResumeId)
            .Throws(new InvalidOperationException("데이터베이스 연결 오류"));

        var json = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/chat/complete", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        errorResponse.GetProperty("error").GetString().ShouldBe("서버 오류가 발생했습니다.");
        errorResponse.GetProperty("message").GetString().ShouldBe("데이터베이스 연결 오류");
        errorResponse.TryGetProperty("timestamp", out _).ShouldBeTrue();
    }

    [Test]
    public async Task InterviewData_WhenKernelServiceThrowsException_ShouldReturnGlobalExceptionResponse()
    {
        // Arrange
        var interviewDataRequest = new InterviewDataRequest
        {
            ResumeId = Guid.NewGuid(),
            JobDescriptionId = Guid.NewGuid(),
            ResumeUrl = "https://example.com/resume.pdf",
            JobDescriptionUrl = "https://example.com/job.pdf"
        };

        _mockKernelService.PreprocessAndInvokeAsync(
                Arg.Any<Guid>(), 
                Arg.Any<Guid>(), 
                Arg.Any<string>(), 
                Arg.Any<string>())
            .Throws(new ArgumentException("잘못된 URL 형식"));

        var json = JsonSerializer.Serialize(interviewDataRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/chat/interview-data", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        errorResponse.GetProperty("error").GetString().ShouldBe("서버 오류가 발생했습니다.");
        errorResponse.GetProperty("message").GetString().ShouldBe("잘못된 URL 형식");
        errorResponse.TryGetProperty("timestamp", out _).ShouldBeTrue();
    }

    [Test]
    public async Task ChatCompletion_WhenSuccessful_ShouldNotTriggerGlobalExceptionHandler()
    {
        // Arrange
        var validResumeId = Guid.NewGuid();
        var validJobId = Guid.NewGuid();
        
        var chatRequest = new ChatRequest
        {
            ResumeId = validResumeId,
            JobDescriptionId = validJobId,
            Messages = new List<ChatMessage>
            {
                new() { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        _mockRepository.GetResumeByIdAsync(validResumeId)
            .Returns(new Models.ResumeEntry { Id = validResumeId, Content = "테스트 이력서" });
        _mockRepository.GetJobByIdAsync(validJobId)
            .Returns(new Models.JobDescriptionEntry { Id = validJobId, Content = "테스트 채용공고" });

        var successResponses = new[] { "안녕하세요, 면접을 시작하겠습니다." };
        _mockKernelService.InvokeInterviewAgentAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<IEnumerable<Microsoft.SemanticKernel.ChatMessageContent>>())
            .Returns(successResponses.ToAsyncEnumerable());

        var json = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/chat/complete", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.ShouldNotContain("서버 오류가 발생했습니다.");
    }

    [Test]
    public async Task NonExistentEndpoint_ShouldReturn404_NotTriggerGlobalExceptionHandler()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private IWebHostBuilder CreateHostBuilder()
    {
        return new WebHostBuilder()
            .UseTestServer()
            .UseEnvironment("Testing")
            .ConfigureServices(services =>
            {
                // 전역 예외 처리기 등록
                services.AddExceptionHandler<GlobalExceptionHandler>();
                services.AddProblemDetails();

                // 라우팅 서비스 추가
                services.AddRouting();

                // 테스트용 모의 서비스 등록
                services.AddScoped(_ => _mockKernelService);
                services.AddScoped(_ => _mockRepository);
                
                // 테스트용 인메모리 데이터베이스
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                services.AddSingleton(connection);
                
                services.AddDbContext<InterviewDbContext>(options =>
                    options.UseSqlite(connection));

                // 기본 모의 객체들 - sealed 클래스들은 실제 인스턴스나 대체 방법 사용
                var kernelBuilder = Kernel.CreateBuilder();
                var testKernel = kernelBuilder.Build();
                services.AddSingleton(testKernel);
                
                services.AddSingleton(Substitute.For<IMcpClient>());
                
                // OpenAIClient도 sealed일 수 있으므로 null 또는 실제 인스턴스 사용
                services.AddSingleton<OpenAIClient>(_ => null!);
                services.AddSingleton(Substitute.For<IUrlContentDownloader>());

                // JSON 직렬화 설정
                services.ConfigureHttpJsonOptions(options =>
                {
                    options.SerializerOptions.WriteIndented = true;
                    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

                // 로깅 설정
                services.AddLogging();
            })
            .Configure(app =>
            {
                // 전역 예외 처리기 사용
                app.UseExceptionHandler();
                
                // 라우팅 사용
                app.UseRouting();
                
                // 엔드포인트 매핑
                app.UseEndpoints(endpoints =>
                {
                    // Chat Completion 엔드포인트 매핑
                    endpoints.MapChatCompletionEndpoint();
                });
            });
    }
}
