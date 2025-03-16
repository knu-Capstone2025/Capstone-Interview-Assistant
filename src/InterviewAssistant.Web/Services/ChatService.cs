// Home.razor에서 쓸 채팅 서비스 구현, api 엔드포인트 관리 등

using System.Net.Http.Json;
using InterviewAssistant.Common.Models;

namespace InterviewAssistant.Web.Services
{
    // 서비스 인터페이스 정의
    public interface IChatService
    {
        Task<ChatResponse?> SendMessageAsync(string message);
        Task ResetChatAsync();
    }

    // 서비스 구현
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;

        // 생성자에서 HttpClient 주입받음
        public ChatService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // 메시지 전송 메서드
        public async Task<ChatResponse?> SendMessageAsync(string message)
        {
            try
            {
                var request = new ChatRequest { Message = message };
                var response = await _httpClient.PostAsJsonAsync("/api/v1/chat", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChatResponse>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // 로깅 등 예외 처리를 할 수 있음
                Console.WriteLine($"Error sending message: {ex.Message}");
                throw; // 호출자에게 예외 전파
            }
        }

        // 채팅 초기화 메서드
        public async Task ResetChatAsync()
        {
            try
            {
                await _httpClient.PostAsync("/api/v1/chat/reset", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting chat: {ex.Message}");
                throw; // 호출자에게 예외 전파
            }
        }
    }
}