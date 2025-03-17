/*
정적 렌더링 중심 테스트
*/

using Bunit;
using InterviewAssistant.Common.Models;
using InterviewAssistant.Web.Components.Pages;
using InterviewAssistant.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;  // MouseEventArgs에 필요한 네임스페이스
using Moq;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;

namespace InterviewAssistant.Web.Tests.Components.Pages
{
    public class HomeTests : Bunit.TestContext
    {
        private readonly Mock<IChatService> _mockChatService;

        public HomeTests()
        {
            // 테스트를 위한 모의 서비스 설정
            _mockChatService = new Mock<IChatService>();
            
            // 테스트 컨텍스트에 모의 서비스 등록
            Services.AddScoped<IChatService>(sp => _mockChatService.Object);
            
            // JavaScript 상호작용 설정 - 느슨한 모드 사용
            JSInterop.Mode = JSRuntimeMode.Loose;
            
            // 알려진 JavaScript 함수 설정
            JSInterop.SetupVoid("eval", _ => true);
            JSInterop.SetupVoid("scrollToBottomWithOffset", _ => true);
            JSInterop.SetupVoid("focusTextArea", _ => true);
        }

        /// <summary>
        /// 컴포넌트가 초기 상태에서 환영 메시지를 올바르게 표시하는지 확인합니다.
        /// </summary>
        [Fact]
        public void Home_InitialRender_ShowsWelcomeMessage()
        {
            // Arrange & Act - 컴포넌트 렌더링
            var cut = RenderComponent<Home>();
            
            // Assert - 환영 메시지가 존재하는지 확인
            cut.FindAll(".welcome-message").Count.Should().Be(1);
            cut.Find(".welcome-message h2").TextContent.Should().Contain("면접 코치 봇에 오신 것을 환영합니다");
        }

        /// <summary>
        /// 입력 영역과 전송 버튼이 올바르게 렌더링되는지 확인합니다.
        /// </summary>
        [Fact]
        public void Home_InitialRender_ShowsInputAreaAndButtons()
        {
            // Arrange & Act - 컴포넌트 렌더링
            var cut = RenderComponent<Home>();
            
            // Assert - 입력 UI 요소 확인
            cut.Find("textarea#messageInput").Should().NotBeNull();
            cut.Find("button.send-btn").Should().NotBeNull();
            cut.Find("button.attachment-btn").Should().NotBeNull();
        }

        /// <summary>
        /// 첨부 버튼을 클릭했을 때 링크 입력 UI가 표시되는지 확인합니다.
        /// </summary>
        [Fact]
        public void Home_AttachmentButton_TogglesProperly()
        {
            // Arrange - 컴포넌트 렌더링
            var cut = RenderComponent<Home>();
            
            // 초기 상태에서는 링크 입력창이 보이지 않음
            cut.FindAll(".file-link-container").Count.Should().Be(0);
            
            // Act - 첨부 버튼 클릭
            var attachButton = cut.Find("button.attachment-btn");
            attachButton.Click();
            
            // Assert - 링크 입력창이 표시됨
            cut.FindAll(".file-link-container").Count.Should().Be(1);
            cut.Find(".link-input-group label").TextContent.Should().Contain("이력서 링크");
            
            // Act - 취소 버튼 클릭
            var cancelButton = cut.Find(".link-buttons button:last-child");
            cancelButton.Click();
            
            // Assert - 링크 입력창이 다시 사라짐
            cut.FindAll(".file-link-container").Count.Should().Be(0);
        }

        /// <summary>
        /// 전송 버튼이 초기에는 비활성화되어 있고, 텍스트가 입력되면 활성화되는지 확인합니다.
        /// </summary>
        [Fact]
        public void Home_SendButton_DisabledWhenNoInput()
        {
            // Arrange & Act - 컴포넌트 렌더링
            var cut = RenderComponent<Home>();
            
            // Assert - 초기 상태에서 전송 버튼은 비활성화
            var sendButton = cut.Find("button.send-btn");
            sendButton.HasAttribute("disabled").Should().BeTrue();
            
            // Act - 텍스트 입력
            var textarea = cut.Find("textarea#messageInput");
            textarea.Input("안녕하세요");
            
            // Assert - 텍스트 입력 후 전송 버튼은 활성화
            sendButton.HasAttribute("disabled").Should().BeFalse();
        }
        
/// <summary>
/// 전송 버튼이 적절하게 설정되어 있는지 확인합니다.
/// </summary>
[Fact]
public void Home_SendButton_IsCorrectlySetup()
{
    // Arrange
    var cut = RenderComponent<Home>();
    
    // Act - 버튼 찾기
    var sendButton = cut.Find("button.send-btn");
    
    // Assert - 버튼이 존재하고 올바른 클래스와 내용을 가지고 있는지 확인
    sendButton.Should().NotBeNull("전송 버튼이 존재해야 합니다");
    sendButton.ClassName.Should().Contain("send-btn", "버튼이 올바른 CSS 클래스를 가져야 합니다");
    sendButton.TextContent.Should().Contain("↵", "버튼이 적절한 아이콘이나 텍스트를 표시해야 합니다");
}
    }
}