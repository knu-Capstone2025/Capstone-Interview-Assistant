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
        
        // 단일 채팅 히스토리
        private static readonly ChatHistory _chatHistory = new ChatHistory();

        public ChatController(ILogger<ChatController> logger, Kernel kernel)
        {
            _logger = logger;
            _kernel = kernel;
            
            // 시스템 메시지 설정 (처음 한 번만)
            if (_chatHistory.Count == 0)
            {
                _chatHistory.AddSystemMessage("당신은 유용한 AI 챗봇입니다. 명확하고 간결하게 답변해주세요.");
            }
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

                // 사용자 메시지 추가
                _chatHistory.AddUserMessage(request.Message);

                // 채팅 완성 설정
                var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 2000,
                    Temperature = 0.7,
                    TopP = 0.95
                };

                // AI에게 응답 요청
                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    _chatHistory,
                    openAIPromptExecutionSettings,
                    _kernel
                );

                // AI 응답을 히스토리에 추가
                _chatHistory.AddAssistantMessage(result.ToString());

                // 응답 생성
                var response = new ChatResponse
                {
                    Response = result.ToString(),
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
            _chatHistory.Clear();
            _chatHistory.AddSystemMessage("당신은 유용한 AI 챗봇입니다. 명확하고 간결하게 답변해주세요.");
            return Ok(new { message = "대화가 초기화되었습니다." });
        }
    }
}