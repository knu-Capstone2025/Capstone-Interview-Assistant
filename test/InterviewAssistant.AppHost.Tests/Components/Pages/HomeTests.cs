/*
HomeTests.cs
Playwright를 사용한 E2E 테스트
*/

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

using NSubstitute;

using NUnit.Framework;

using Shouldly;

namespace InterviewAssistant.AppHost.Tests.Components.Pages
{
    [TestFixture]
    public class HomeTests : PageTest
    {
        //private IChatService _mockChatService;
        private string _baseUrl = string.Empty;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // CI 환경 감지: GitHub Actions에서는 GITHUB_ACTIONS 환경 변수가 설정됩니다
            // if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            // {
            //     // gitjub action 에서도 실행되어야 함
            //     //Assert.Ignore("CI 환경에서는 E2E 테스트를 실행하지 않습니다.");
            // }

            // .NET Aspire 테스트 환경에서는 일반적으로 ASPNETCORE_URLS 환경 변수에 URL이 설정됩니다
            string? aspireUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

            if (!string.IsNullOrEmpty(aspireUrl) && aspireUrl.Contains(";"))
            {
                // 여러 URL이 설정된 경우 첫 번째 URL 사용
                _baseUrl = aspireUrl.Split(';')[0];
                Console.WriteLine($"Aspire URL 사용: {_baseUrl}");
            }
            else
            {
                // 환경 변수가 없으면 기본 URL 사용
                _baseUrl = "http://localhost:5168";
                Console.WriteLine($"기본 URL 사용: {_baseUrl}");
            }
            await Task.CompletedTask;

        }

        [SetUp]
        public async Task Setup() //비동기 프로그램 구현
        {
            //Arrange
            await Page.GotoAsync(_baseUrl); //지정된 URL(_baseUrl)로 이동
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle); //페이지가 완전히 로드될 때까지 대기
            // 모의 서비스 설정
            //_mockChatService = Substitute.For<IChatService>();
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

        /// <summary>
        /// 전송 버튼이 초기에는 비활성화되어 있고, 텍스트가 입력되면 활성화되는지 확인합니다.
        /// </summary>
        [Test]
        public async Task Home_SendButton_DisabledWhenNoInput()
        {
            // Arrange
            // SetUp에서 이미 페이지가 로드되어 있으므로 별도의 로딩 동작은 필요 없음
            var sendButton = Page.Locator("button.send-btn");
            var textarea = Page.Locator("textarea#messageInput");

            // Act
            // 초기 상태에서 전송 버튼은 비활성화 (Locator 기반으로 변경)
            await Expect(sendButton).ToBeVisibleAsync();
            await Expect(sendButton).ToBeDisabledAsync();

            // 텍스트 입력 필드 찾기 (Locator 기반으로 변경)
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
    }
}