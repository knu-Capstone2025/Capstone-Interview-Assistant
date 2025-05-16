using System.Text.RegularExpressions;

namespace InterviewAssistant.Web.Utilities
{
    public static class SecurityValidator
    {
        private static readonly string[] BlockedKeywords = new[]
        {
            "system.prompt",
            "ignore previous",
            "ignore above",
            "forget",
            "new persona",
            "you are now",
            "you're now",
            "act as",
            "ignore instructions",
            "assistant terminated",
            "system command",
            "overwrite instructions"
        };

        private const int MaxMessageLength = 1000;  // 최대 메시지 길이
        private const int MaxUrlLength = 2000;      // 최대 URL 길이        
        public static (bool IsValid, string? ErrorMessage) ValidateMessage(string message)
        {
            if (message.Length > MaxMessageLength)
                return (false, $"메시지가 너무 깁니다. (최대 {MaxMessageLength}자)");

            // 위험한 키워드 확인
            foreach (var keyword in BlockedKeywords)
            {
                if (message.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return (false, "메시지에 허용되지 않는 키워드가 포함되어 있습니다.");
            }

            // 특수 문자 및 이스케이프 시퀀스 패턴 검사 (마크다운 문법은 허용)
            var dangerousPattern = @"[<>]|javascript:|data:|file:|vbscript:|\\x[0-9a-fA-F]{2}|\\u[0-9a-fA-F]{4}";
            if (Regex.IsMatch(message, dangerousPattern, RegexOptions.IgnoreCase))
                return (false, "메시지에 허용되지 않는 문자가 포함되어 있습니다.");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (false, "URL이 비어있습니다.");

            if (url.Length > MaxUrlLength)
                return (false, $"URL이 너무 깁니다. (최대 {MaxUrlLength}자)");

            // 위험한 프로토콜 확인
            var dangerousProtocols = new[] { "javascript:", "data:", "vbscript:", "file:" };
            if (dangerousProtocols.Any(protocol => url.StartsWith(protocol, StringComparison.OrdinalIgnoreCase)))
                return (false, "허용되지 않는 URL 프로토콜입니다.");

            // URL 형식 검증
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return (false, "올바른 HTTP/HTTPS URL 형식이 아닙니다.");
            }

            return (true, null);
        }

        public static string SanitizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            // HTML 태그 이스케이프
            message = message.Replace("<", "&lt;").Replace(">", "&gt;");

            // 길이 제한
            if (message.Length > MaxMessageLength)
            {
                message = message[..MaxMessageLength];
            }

            return message.Trim();
        }
    }
}
