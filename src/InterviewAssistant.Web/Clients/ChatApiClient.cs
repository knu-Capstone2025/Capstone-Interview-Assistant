// ChatApiClient.cs

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using InterviewAssistant.Common.Models;
using Microsoft.Extensions.Logging;

namespace InterviewAssistant.Web.Clients
{
    /// <summary>
    /// 백엔드 채팅 API와의 통신을 담당하는 클라이언트 인터페이스
    /// </summary>
    public interface IChatApiClient
    {
        /// <summary>
        /// 메시지를 백엔드 API로 전송합니다.
        /// </summary>
        /// <param name="request">전송할 채팅 요청</param>
        /// <returns>API 응답 또는 오류 시 null</returns>
        Task<ChatResponse?> SendMessageAsync(ChatRequest request);
        
        /// <summary>
        /// 채팅 세션을 초기화합니다.
        /// </summary>
        Task ResetChatAsync();
    }

    /// <summary>
    /// 백엔드 채팅 API와의 통신을 담당하는 클라이언트 구현
    /// </summary>
    public class ChatApiClient : IChatApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatApiClient> _logger;
        
        // API 엔드포인트 상수
        private const string SendMessageEndpoint = "/api/v1/chat";
        private const string ResetChatEndpoint = "/api/v1/chat/reset";
        
        public ChatApiClient(HttpClient httpClient, ILogger<ChatApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc/>
        public async Task<ChatResponse?> SendMessageAsync(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("API 요청 전송: {Message}", request.Message);
                
                var response = await _httpClient.PostAsJsonAsync(SendMessageEndpoint, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                    _logger.LogInformation("API 응답 성공");
                    return result;
                }
                
                _logger.LogWarning("API 응답 실패: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 전송 중 오류 발생");
                throw; // 호출자에게 예외 전파
            }
        }
        
        /// <inheritdoc/>
        public async Task ResetChatAsync()
        {
            try
            {
                _logger.LogInformation("채팅 초기화 요청 전송");
                
                var response = await _httpClient.PostAsync(ResetChatEndpoint, null);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("채팅 초기화 API 응답 실패: {StatusCode}", response.StatusCode);
                }
                else
                {
                    _logger.LogInformation("채팅 초기화 성공");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 초기화 중 오류 발생");
                throw; // 호출자에게 예외 전파
            }
        }
    }
}