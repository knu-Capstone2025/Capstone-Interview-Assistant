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
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        [SetUp]
        public async Task Setup()
        {
            // GitHub Actions 환경에서는 테스트 건너뛰기
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            {
                Assert.Ignore("CI 환경에서는 E2E 테스트를 실행하지 않습니다.");
                return;
            }
            
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InterviewAssistant_AppHost>();

            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });

            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            await using var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
            await app.StartAsync().WaitAsync(DefaultTimeout);

            // 웹 리소스 찾기
            var webResource = appHost.Resources.FirstOrDefault(r => r.Name == "webfrontend");
            Assert.That(webResource, Is.Not.Null, "webfrontend 리소스를 찾을 수 없습니다.");

            // 엔드포인트 찾기
            var endpoint = webResource.Annotations.OfType<EndpointAnnotation>()
                                     .FirstOrDefault(x => x.Name == "http");
            Assert.That(endpoint, Is.Not.Null, "HTTP 엔드포인트를 찾을 수 없습니다.");

            // Playwright로 페이지 이동
            var uriString = endpoint?.AllocatedEndpoint?.UriString ?? "http://localhost:5168";
            await Page.GotoAsync(uriString);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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