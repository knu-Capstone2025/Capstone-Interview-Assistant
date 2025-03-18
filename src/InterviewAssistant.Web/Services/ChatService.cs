// ChatService.cs

using System;
using System.Threading.Tasks;
using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Clients;
using Microsoft.Extensions.Logging;

namespace InterviewAssistant.Web.Services
{
    /// <summary>
    /// 채팅 관련 비즈니스 로직을 처리하는 서비스 인터페이스
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 사용자 메시지를 처리하고 응답을 반환합니다.
        /// </summary>
        /// <param name="message">사용자 메시지</param>
        /// <returns>처리된 챗봇 응답</returns>
        Task<ChatResponse?> SendMessageAsync(string message);
        
        /// <summary>
        /// 새로운 채팅 세션을 시작합니다.
        /// </summary>
        Task ResetChatAsync();
    }

    /// <summary>
    /// 채팅 관련 비즈니스 로직을 처리하는 서비스 구현
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IChatApiClient _chatApiClient;
        private readonly ILogger<ChatService> _logger;
        
        public ChatService(IChatApiClient chatApiClient, ILogger<ChatService> logger)
        {
            _chatApiClient = chatApiClient ?? throw new ArgumentNullException(nameof(chatApiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc/>
        public async Task<ChatResponse?> SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("빈 메시지가 전송되었습니다.");
                return null;
            }
            
            _logger.LogInformation("메시지 처리 시작: {MessagePreview}", 
                message.Length <= 50 ? message : message.Substring(0, 50) + "...");
            
            // 메시지 전처리 로직 (향후 확장 가능)
            // 예: 링크 감지, 특수 명령어 처리 등
            string processedMessage = message.Trim();
            
            // API 요청 생성 및 전송
            var request = new ChatRequest { Message = processedMessage };
            var response = await _chatApiClient.SendMessageAsync(request);
            
            // 응답 후처리 로직 (향후 확장 가능)
            // 예: 응답 포맷팅, 추가 정보 주입 등
            
            _logger.LogInformation("메시지 처리 완료");
            return response;
        }
        
        /// <inheritdoc/>
        public async Task ResetChatAsync()
        {
            _logger.LogInformation("채팅 세션 초기화 시작");
            
            // 세션 초기화 전 로직 (향후 확장 가능)
            // 예: 로컬 상태 초기화, 사용자 설정 로드 등
            
            await _chatApiClient.ResetChatAsync();
            
            // 세션 초기화 후 로직 (향후 확장 가능)
            // 예: 초기 메시지 설정, 상태 업데이트 등
            
            _logger.LogInformation("채팅 세션 초기화 완료");
        }
    }
}