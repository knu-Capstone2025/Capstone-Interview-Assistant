using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json.Serialization;

namespace InterviewAssistant.ApiService.Controllers
{
    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [ApiController]
    [Route("api/v1/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly Kernel _kernel;
        
        // 매우 간단한 히스토리 (최근 메시지 2개만 유지)
        private static string _lastUserMessage = string.Empty;
        private static string _lastBotResponse = string.Empty;
        
        // 속도 제한을 위한 타임스탬프
        private static DateTime _lastRequestTime = DateTime.MinValue;
        private static readonly SemaphoreSlim _throttleLock = new SemaphoreSlim(1, 1);

        public ChatController(ILogger<ChatController> logger, Kernel kernel)
        {
            _logger = logger;
            _kernel = kernel;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> PostMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest("메시지 내용이 비어있습니다.");
                }
                
                // 요청 스로틀링 적용 - 최소 60초 간격으로 요청 제한
                await _throttleLock.WaitAsync();
                try
                {
                    var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                    if (timeSinceLastRequest.TotalSeconds < 60) // 오류 메시지에서 권장한 60초 간격
                    {
                        var waitTime = TimeSpan.FromSeconds(60) - timeSinceLastRequest;
                        _logger.LogInformation("Rate limiting: Waiting for {WaitTime}ms before next request", waitTime.TotalMilliseconds);
                        await Task.Delay(waitTime);
                    }
                    
                    _lastRequestTime = DateTime.UtcNow;
                }
                finally
                {
                    _throttleLock.Release();
                }

                // 최근 메시지만 사용하는 새로운 채팅 히스토리 생성
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("당신은 유용한 AI 챗봇입니다. 짧고 명확하게 답변해주세요.");
                
                // 이전 대화가 있으면 추가
                if (!string.IsNullOrEmpty(_lastUserMessage) && !string.IsNullOrEmpty(_lastBotResponse))
                {
                    chatHistory.AddUserMessage(_lastUserMessage);
                    chatHistory.AddAssistantMessage(_lastBotResponse);
                }
                
                // 현재 메시지 추가
                chatHistory.AddUserMessage(request.Message);
                _lastUserMessage = request.Message;

                // 짧은 응답을 위한 설정
                var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 200,  // 응답 길이 제한을 더 줄임
                    Temperature = 0.7,
                    TopP = 0.95
                };

                string responseText;
                try {
                    // AI 응답 요청
                    var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
                    var result = await chatCompletionService.GetChatMessageContentAsync(
                        chatHistory,
                        openAIPromptExecutionSettings,
                        _kernel
                    );
                    
                    responseText = result.ToString();
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error calling AI service");
                    // 실패 시 간단한 응답 제공
                    responseText = "죄송합니다. 현재 시스템이 바쁩니다. 잠시 후 다시 시도해주세요.";
                }
                
                _lastBotResponse = responseText;

                // 응답 생성
                var response = new ChatResponse
                {
                    Response = responseText,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Chat response generated");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat request processing failed");
                return StatusCode(500, $"내부 서버 오류: {ex.Message}");
            }
        }

        // 대화 초기화 엔드포인트
        [HttpPost("reset")]
        public ActionResult ResetChat()
        {
            _lastUserMessage = string.Empty;
            _lastBotResponse = string.Empty;
            return Ok(new { message = "대화가 초기화되었습니다." });
        }
    }
}