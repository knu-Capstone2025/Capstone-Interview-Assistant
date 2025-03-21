// ChatApiClient.cs

using System;
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
    }

    /// <summary>
    /// 백엔드 채팅 API와의 통신을 담당하는 클라이언트 구현
    /// </summary>
    public class ChatApiClient(ILoggerFactory loggerFactory) : IChatApiClient
    {
        // ILoggerFactory를 사용하여 로거 생성, null 체크 포함
        private readonly ILogger<ChatApiClient> _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                                                          .CreateLogger<ChatApiClient>();
            
        // 하드코딩된 응답
        private readonly string _hardcodedResponse = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
        
        /// <inheritdoc/>
        public async Task<ChatResponse?> SendMessageAsync(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("API 요청 시뮬레이션: {Message}", 
                    request.Messages.LastOrDefault()?.Message ?? "빈 메시지");
                
                // 네트워크 지연 시뮬레이션
                await Task.Delay(1000);
                
                // 하드코딩된 응답 반환
                var response = new ChatResponse { Message = _hardcodedResponse };
                
                _logger.LogInformation("API 응답 시뮬레이션 완료");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 처리 중 오류 발생");
                return null;
            }
        }
    }
}