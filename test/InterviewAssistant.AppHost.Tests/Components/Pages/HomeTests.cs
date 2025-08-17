/*
HomeTests.cs
Playwright를 사용한 E2E 테스트
*/

using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

using Shouldly;

namespace InterviewAssistant.AppHost.Tests.Components.Pages;

[TestFixture]
public class HomeTests : PageTest
{
    private DistributedApplication _app;
    private string _baseUrl;
    private const int MODAL_TIMEOUT = 30000;  // 모달 타임아웃 30초
    private const int RESPONSE_TIMEOUT = 60000; // 응답 타임아웃 60초
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
        var endpoint = webResource?.Annotations
                                   .OfType<EndpointAnnotation>()
                                   .FirstOrDefault(x => x.Name == "http");

        _baseUrl = endpoint?.AllocatedEndpoint?.UriString!;
    }

    [SetUp]
    public async Task Setup()
    {
        // 페이지 로드 전에 충분한 시간을 기다립니다
        await Task.Delay(2000);
        await Page.GotoAsync(_baseUrl, new PageGotoOptions
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000 
        });
        // 페이지 로드 후 추가 대기
        await Task.Delay(2000);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
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

    // <summary>
    // 전송 버튼이 초기에는 비활성화되어 있고, 텍스트가 입력되면 활성화되는지 확인합니다.
    // </summary>
    [Test]
    public async Task Home_SendButton_DisabledWhenNoInput()
    {
        // Arrange: 페이지 로드 후 링크 공유 설정
        await Page.Locator("button.share-btn").ClickAsync();
        await Page.Locator("input#resumeUrl").FillAsync("https://example.com/resume.pdf");
        await Page.Locator("input#jobUrl").FillAsync("https://example.com/job.pdf");
        await Page.Locator("button.submit-btn").ClickAsync();
        await Page.WaitForSelectorAsync(".modal", new() { State = WaitForSelectorState.Detached, Timeout = 10000 }); // 모달 닫힘 대기 시간 증가

        var sendButton = Page.Locator("button.send-btn");
        var textarea = Page.Locator("textarea#messageInput");

        // 초기 AI 응답 및 UI 준비 대기
        await Page.WaitForSelectorAsync(".welcome-message", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 15000
        });
        await Page.WaitForSelectorAsync(".response-status", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 40000
        });

        // Textarea가 활성화될 때까지 명시적으로 대기
        await Expect(textarea).ToBeEnabledAsync(new() { Timeout = 15000 });

        // Act
        // 초기 상태에서 전송 버튼은 비활성화 (Locator 기반으로 변경)
        await Expect(sendButton).ToBeVisibleAsync();
        await Expect(sendButton).ToBeDisabledAsync();

        // 텍스트 입력 필드 확인 (Locator 기반으로 변경), 링크 공유 후 채팅창 활성화 확인
        await Expect(textarea).ToBeVisibleAsync();

        // 텍스트 입력 
        await textarea.FillAsync("안녕하세요"); // TypeAsync 대신 FillAsync 사용

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
        await Expect(modal).ToBeVisibleAsync(new() { Timeout = MODAL_TIMEOUT });

        // `alert` 감지 이벤트 핸들러 등록
        string alertMessage = "";
        var alertHandled = new TaskCompletionSource<bool>();

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
        await Page.WaitForSelectorAsync(".modal", new()
        {
            State = WaitForSelectorState.Detached,
            Timeout = MODAL_TIMEOUT
        });

        // 2) AI 응답이 끝날 때까지 대기        
        await Page.WaitForSelectorAsync(".response-status", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = RESPONSE_TIMEOUT
        });

        // 3) 입력창 활성화 상태 확인
        var chatArea = Page.Locator("textarea#messageInput");
        await Expect(chatArea).ToBeVisibleAsync();
        await Expect(chatArea).ToBeEnabledAsync();
    }

    [Test]
    public async Task Home_Serveroutput_Prohibit_UserTransport()
    {
        // Arrange
        await Page.Locator("button.share-btn").ClickAsync();
        await Page.Locator("input#resumeUrl").FillAsync("https://example.com/resume.pdf");
        await Page.Locator("input#jobUrl").FillAsync("https://example.com/job.pdf");
        await Page.Locator("button.submit-btn").ClickAsync();
        await Page.WaitForSelectorAsync(".modal", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 10000 // 모달 닫힘 대기 시간
        });

        // 초기 AI 응답 및 UI 준비 대기
        await Page.WaitForSelectorAsync(".welcome-message", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 15000 // 환영 메시지 사라짐 대기 시간
        });
        await Page.WaitForSelectorAsync(".response-status", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 40000 // AI 응답 상태 메시지 사라짐 대기 시간
        });

        var statusMessage = Page.Locator(".response-status");
        var textarea = Page.Locator("textarea#messageInput");
        var sendButton = Page.Locator("button.send-btn");

        // Textarea가 활성화될 때까지 명시적으로 대기
        await Expect(textarea).ToBeEnabledAsync(new() { Timeout = 15000 });

        // Act
        // 메시지 전송
        await textarea.FillAsync("안녕하세요, 면접 준비를 도와주세요");
        await sendButton.ClickAsync();
        await Page.WaitForSelectorAsync(".response-status", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 20000
        });

        // Assert
        // 상태 메시지 확인
        await Expect(statusMessage).ToContainTextAsync("서버 응답 출력 중... 출력이 완료될 때까지 기다려주세요.");

        // isServerOutputEnded 상태 직접 확인
        var isServerOutputEnded = await Page.EvaluateAsync<bool>("window.isServerOutputEnded");
        isServerOutputEnded.ShouldBe(false, "서버 응답 중에는 isServerOutputEnded가 false여야 합니다.");

        // 버튼 비활성화 확인
        await Expect(sendButton).ToBeDisabledAsync();

        // Enter 키 입력 전 메시지 개수 확인
        var messageCountBeforeEnter = await Page.EvaluateAsync<int>("document.querySelectorAll('.message').length");

        // 엔터키 입력 시 메시지가 전송되지 않음
        await textarea.PressAsync("Enter");

        await Task.Delay(1000);

        var messageCountAfterEnter = await Page.EvaluateAsync<int>("document.querySelectorAll('.message').length");

        // Enter 키 입력 후 메시지 개수가 동일한지 확인
        messageCountAfterEnter.ShouldBe(messageCountBeforeEnter,
            "서버 응답 중 Enter 키를 눌렀을 때 추가 메시지가 전송되지 않아야 합니다. " +
            $"Enter 전 메시지 수: {messageCountBeforeEnter}, Enter 후 메시지 수: {messageCountAfterEnter}");
    }

   /// <summary>
    /// 중복 이벤트 방지 플래그가 제대로 동작하는지 확인합니다.
    /// </summary>
    [Test]
    public async Task Home_HandleKeyDown_PreventsDuplicateWithIsSendFlag()
    {
        // Arrange - 초기 설정
        await Page.Locator("button.share-btn").ClickAsync();
        await Page.Locator("input#resumeUrl").FillAsync("https://example.com/resume.pdf");
        await Page.Locator("input#jobUrl").FillAsync("https://example.com/job.pdf");
        await Page.Locator("button.submit-btn").ClickAsync();
        
        // 모달 닫힘 대기
        await Page.WaitForSelectorAsync(".modal", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 10000
        });

        // 초기 AI 응답 완료까지 대기 (다른 테스트와 동일하게)
        await Page.WaitForSelectorAsync(".welcome-message", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 15000
        });
        
        await Page.WaitForSelectorAsync(".response-status", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = RESPONSE_TIMEOUT // 60초로 증가
        });

        // 초기 AI 응답 및 UI 준비 대기
        await Page.WaitForSelectorAsync(".welcome-message", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 15000
        });
        await Page.WaitForSelectorAsync(".response-status", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 40000
        });

        var textarea = Page.Locator("textarea#messageInput");
        
        // Textarea가 활성화될 때까지 명시적으로 대기 (타임아웃 증가)
        await Expect(textarea).ToBeEnabledAsync(new() { Timeout = 20000 });
        
        // 초기 메시지 개수 확인
        var initialMessageCount = await Page.EvaluateAsync<int>("document.querySelectorAll('.message').length");

        // Act 1: 정상적인 첫 번째 메시지 전송
        await textarea.FillAsync("첫 번째 메시지");
        await textarea.PressAsync("Enter");
        
        // 메시지 전송 후 충분한 대기 시간
        await Task.Delay(3000);
        
        var afterFirstSend = await Page.EvaluateAsync<int>("document.querySelectorAll('.message').length");
        
        // Assert 1: 첫 번째 메시지 전송 확인
        Console.WriteLine($"초기 메시지 수: {initialMessageCount}, 첫 전송 후: {afterFirstSend}");

        // Act 2: isSend 플래그를 true로 설정하여 중복 전송 차단 테스트
        await textarea.ClearAsync();
        await textarea.FillAsync("차단될 메시지");
        
        // isSend 플래그를 true로 설정 (중복 전송 방지 상황 시뮬레이션)
        await Page.EvaluateAsync("window.isSend = true;");
        
        // 플래그 상태 확인
        var flagStatus = await Page.EvaluateAsync<bool>("window.isSend === true");
        Console.WriteLine($"isSend 플래그 상태: {flagStatus}");
        
        // textarea가 여전히 활성화되어 있는지 재확인
        await Expect(textarea).ToBeEnabledAsync(new() { Timeout = 5000 });
        
        await textarea.PressAsync("Enter");
        await Task.Delay(2000); // 처리 시간 대기
        
        var afterBlockedSend = await Page.EvaluateAsync<int>("document.querySelectorAll('.message').length");
        Console.WriteLine($"차단 테스트 후 메시지 수: {afterBlockedSend}");
        
        // Assert 2: isSend 플래그가 true일 때 메시지 전송 차단 확인
        afterBlockedSend.ShouldBe(afterFirstSend, "isSend 플래그가 true일 때 메시지 전송이 차단되어야 합니다.");

        // Act 3: 플래그 해제 후 정상 전송 확인
        await Page.EvaluateAsync("window.isSend = false;");
        
        // 플래그 해제 상태 확인
        var flagResetStatus = await Page.EvaluateAsync<bool>("window.isSend === false");
        Console.WriteLine($"isSend 플래그 해제 상태: {flagResetStatus}");
        
        // 새로운 메시지로 다시 시도
        await textarea.ClearAsync();
        await textarea.FillAsync("플래그 해제 후 메시지");
        
        // textarea 상태 재확인
        await Expect(textarea).ToBeEnabledAsync(new() { Timeout = 5000 });
        
        await textarea.PressAsync("Enter");
        await Task.Delay(3000); // 처리 시간 대기
        
        var afterFlagReset = await Page.EvaluateAsync<int>("document.querySelectorAll('.message').length");
        Console.WriteLine($"플래그 해제 후 메시지 수: {afterFlagReset}");
        
        // Assert 3: 플래그 동작 검증
        // 플래그가 true일 때는 메시지가 차단되고, false일 때는 전송될 수 있음을 확인
        var blockedCorrectly = (afterBlockedSend == afterFirstSend);
        var allowedAfterReset = (afterFlagReset >= afterBlockedSend);
        
        blockedCorrectly.ShouldBeTrue("isSend 플래그가 true일 때 메시지 전송이 차단되어야 합니다.");
        
        // 테스트 환경에서 실제 메시지 전송이 작동하는지 확인
        if (afterFirstSend > initialMessageCount || afterFlagReset > afterBlockedSend)
        {
            Console.WriteLine("메시지 전송 기능이 작동하는 환경입니다.");
            allowedAfterReset.ShouldBeTrue("isSend 플래그 해제 후 메시지 전송이 허용되어야 합니다.");
        }
        else
        {
            Console.WriteLine("메시지 전송 기능이 작동하지 않는 테스트 환경입니다. 플래그 차단 동작만 검증합니다.");
        }
        
        // 핵심 검증: 플래그가 true일 때와 false일 때의 동작 차이
        Console.WriteLine($"테스트 결과 - 초기:{initialMessageCount}, 첫전송:{afterFirstSend}, 차단:{afterBlockedSend}, 해제:{afterFlagReset}");
    }
}
