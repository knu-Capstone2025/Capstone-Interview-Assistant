/*
HomeTests.cs
Playwright를 사용한 E2E 테스트
*/

using System;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

using NSubstitute;
using NUnit.Framework;
using Shouldly;

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Lifecycle;

namespace InterviewAssistant.AppHost.Tests.Components.Pages
{
    [TestFixture]
    public class HomeTests : PageTest
    {
        private DistributedApplication _app;
        private string _baseUrl;
        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            };
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {

            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InterviewAssistant_AppHost>();
            _app = appHost.Build();
            await _app.StartAsync();

            var webResource = appHost.Resources.FirstOrDefault(r => r.Name == "webfrontend");
            var endpoint = webResource?.Annotations.OfType<EndpointAnnotation>()
                .FirstOrDefault(x => x.Name == "http");

            _baseUrl = endpoint?.AllocatedEndpoint?.UriString!;
        }
        
        [SetUp]
        public async Task Setup()
        {
            await Page.GotoAsync(_baseUrl);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        /// <summary>
        /// 컴포넌트가 초기 상태에서 환영 메시지를 올바르게 표시하는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_InitialRender_ShowsWelcomeMessage()
        {
            // Arrange : 페이지 접속 및 로드 완료 대기 (Setup 메서드에서 수행)

            // Act : 환영 메시지 확인 (Locator 기반으로 변경)
            var welcomeMessage = Page.Locator(".welcome-message");
            await Expect(welcomeMessage).ToBeVisibleAsync();

            var heading = Page.Locator(".welcome-message h2");
            await Expect(heading).ToBeVisibleAsync();

            // Assert : 환영 메시지 확인
            var headingText = await heading.TextContentAsync();
            headingText?.ShouldContain("면접 코치 봇에 오신 것을 환영합니다", Case.Insensitive, "환영 메시지가 올바른 내용을 포함해야 합니다");
        }

        /// <summary>
        /// 입력 영역과 전송 버튼이 올바르게 렌더링되는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_InitialRender_ShowsInputAreaAndButtons()
        {
            // Arrange : 페이지 접속 및 로드 완료 대기 (Setup 메서드에서 수행)

            // Act : 입력 UI 요소 확인 (Locator 기반으로 변경)
            var textarea = Page.Locator("textarea#messageInput");
            var sendButton = Page.Locator("button.send-btn");

            // Assert : UI 요소 확인
            await Expect(textarea).ToBeVisibleAsync();
            await Expect(sendButton).ToBeVisibleAsync();

        }

        // // <summary>
        // // 전송 버튼이 초기에는 비활성화되어 있고, 텍스트가 입력되면 활성화되는지 확인합니다.
        // // </summary>
        [Test]
        public async Task Home_SendButton_DisabledWhenNoInput()
        {
            // Arrange: 페이지 로드 후 링크 공유 설정
            await Page.Locator("button.share-btn").ClickAsync();
            await Page.Locator("input#resumeUrl").FillAsync("https://example.com/resume.pdf");
            await Page.Locator("input#jobUrl").FillAsync("https://example.com/job.pdf");
            await Page.Locator("button.submit-btn").ClickAsync();
            await Task.Delay(1000); // UI 반영 대기

            // 디버깅용: isLinkShared 값 확인
            var isLinkShared = await Page.EvaluateAsync<bool>("window.isLinkShared");
            Console.WriteLine($"isLinkShared 값: {isLinkShared}");

            var sendButton = Page.Locator("button.send-btn");
            var textarea = Page.Locator("textarea#messageInput");

            // Act
            // 초기 상태에서 전송 버튼은 비활성화 (Locator 기반으로 변경)
            await Expect(sendButton).ToBeVisibleAsync();
            await Expect(sendButton).ToBeDisabledAsync();
            
            // 텍스트 입력 필드 확인 (Locator 기반으로 변경)
            await Expect(textarea).ToBeVisibleAsync();

            // 텍스트 입력 전 잠시 대기
            await Task.Delay(500);

            // 텍스트 필드 클릭하고 내용 지우기
            await textarea.ClickAsync();
            await textarea.FillAsync(""); // 내용 비우기 - TypeAsync 대신 FillAsync 사용

            // 텍스트 입력 
            await textarea.FillAsync("안녕하세요"); // TypeAsync 대신 FillAsync 사용

            // 입력 완료 후 UI 업데이트를 위한 시간 제공
            await Task.Delay(1000);

            // 디버깅용: 입력된 텍스트 확인
            var textareaValue = await Page.EvaluateAsync<string>("document.querySelector('textarea#messageInput').value");
            Console.WriteLine($"입력된 텍스트: '{textareaValue}'");

            // 디버깅용: 버튼 상태 확인
            var buttonDisabled = await Page.EvaluateAsync<bool>("document.querySelector('button.send-btn').disabled");
            Console.WriteLine($"버튼 비활성화 상태: {buttonDisabled}");

            // Assert
            // 텍스트 입력 후 전송 버튼은 활성화되어야 함
            await Expect(sendButton).ToBeEnabledAsync();
        }


        /// <summary>
        /// 전송 버튼이 적절하게 설정되어 있는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_SendButton_IsCorrectlySetup()
        {
            // Arrange
            var sendButton = Page.Locator("button.send-btn");

            // Act
            // 버튼 찾기 및 확인 (Locator 기반으로 변경)
            await Expect(sendButton).ToBeVisibleAsync();

            // 클래스 확인
            var buttonClass = await sendButton.GetAttributeAsync("class");

            // 텍스트 확인
            var buttonText = await sendButton.TextContentAsync();

            // Assert
            // CSS 클래스 확인
            buttonClass?.ShouldContain("send-btn", Case.Insensitive, "버튼이 올바른 CSS 클래스를 가져야 합니다");

            // 텍스트 확인
            buttonText?.ShouldContain("↵", Case.Insensitive, "버튼이 적절한 아이콘이나 텍스트를 표시해야 합니다");
        }

        /// <summary>
        /// 입력 영역이 비활성화되어 있는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_InputArea_DisabledWhenLinkNotShared()
        {
            // Arrange
            await Page.EvaluateAsync("window.isLinkShared = false;");
            var textarea = Page.Locator("textarea#messageInput");

            // Act & Assert
            await Expect(textarea).ToBeVisibleAsync();
            await Expect(textarea).ToBeDisabledAsync(); // 입력창 비활성화 확인
        }

        /// <summary>
        /// 링크 공유 버튼이 올바르게 렌더링되는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_LinkShareButton_ShowsCorrectly()
        {
            // Arrange
            var linkShareButton = Page.Locator("button.share-btn");

            // Act & Assert
            await Expect(linkShareButton).ToBeVisibleAsync();
            await Expect(linkShareButton).ToHaveTextAsync("이력서 및 채용공고 공유하기");
        }

        /// <summary>
        /// 링크 공유 버튼 클릭 시 링크 공유 모달 창의 UI가 올바르게 생성되는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_LinkShareButton_Click_ShowsModal()
        {
            // Arrange
            var linkShareButton = Page.Locator("button.share-btn");
            var modal = Page.Locator(".modal");

            // Act
            await linkShareButton.ClickAsync();

            // Assert
            await Expect(modal).ToBeVisibleAsync();
            await Expect(modal.Locator("h3")).ToHaveTextAsync("링크 공유");
            await Expect(modal.Locator("button.close-btn")).ToBeVisibleAsync();
            await Expect(modal.Locator("button.submit-btn")).ToBeVisibleAsync();
            await Expect(modal.Locator("input#resumeUrl")).ToBeVisibleAsync();
            await Expect(modal.Locator("input#jobUrl")).ToBeVisibleAsync();
        }

        /// <summary>
        /// 모달 창에서 URL이 유효하지 않을 때 경고 메시지가 표시되는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_LinkShareButton_Click_ShowsModal_Error()
        {
            // Arrange
            var linkShareButton = Page.Locator("button.share-btn");
            var modal = Page.Locator(".modal");
            var submitButton = modal.Locator("button.submit-btn");

            // Act
            await linkShareButton.ClickAsync();
            await Expect(modal).ToBeVisibleAsync(); // Timeout error : 모달이 열릴 때까지 대기

            // `alert` 감지 이벤트 핸들러 등록
            string alertMessage = "";
            TaskCompletionSource<bool> alertHandled = new TaskCompletionSource<bool>();

            Page.Dialog += async (_, dialog) =>
            {
                alertMessage = dialog.Message;
                await dialog.DismissAsync(); // alert 닫기
                alertHandled.SetResult(true); // alert 핸들러가 호출되었음을 표시
            };

            await submitButton.ClickAsync();

            // Assert
            await alertHandled.Task;
            alertMessage.ShouldBe("URL이 유효하지 않습니다. 다시 확인해주세요.");
        }

        /// <summary>
        /// 링크 공유 후 채팅창이 활성화 되는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_LinkShareButton_Click_ActivatesChat()
        {
           // Arrange
            var linkShareButton = Page.Locator("button.share-btn");
            var modal = Page.Locator(".modal");
            var submitButton = modal.Locator("button.submit-btn");

            await linkShareButton.ClickAsync(); // 모달 창 열기
            await Expect(modal).ToBeVisibleAsync(); // 모달이 제대로 열렸는지 확인

            // 입력 필드에 URL 입력
            var resumeUrlInput = modal.Locator("input#resumeUrl");
            var jobUrlInput = modal.Locator("input#jobUrl");
            await resumeUrlInput.FillAsync("https://example.com/resume.pdf");
            await jobUrlInput.FillAsync("https://example.com/job-posting");

            await submitButton.ClickAsync(); // 모달 창 닫기
            await Expect(modal).Not.ToBeVisibleAsync(); // 모달이 닫혔는지 확인

            // Assert : 채팅창 활성화 확인
            var chatArea = Page.Locator("textarea#messageInput");
            await Expect(chatArea).ToBeVisibleAsync();
        }
    }
}