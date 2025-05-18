using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using InterviewAssistant.ApiService.Delegates;
using InterviewAssistant.ApiService.Models;
using InterviewAssistant.ApiService.Repositories;
using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using NUnit.Framework;

using Shouldly;

namespace InterviewAssistant.ApiService.Tests.Delegates;

[TestFixture]
public class ChatCompletionDelegateTests
{
    private IKernelService _kernelService;
    private IInterviewRepository _repository;
    private Guid _validResumeId;
    private Guid _validJobDescriptionId;
    private ResumeEntry _resumeEntry;
    private JobDescriptionEntry _jobDescriptionEntry;
    private const string TestResumeContent = "테스트 이력서 내용";
    private const string TestJobDescriptionContent = "테스트 직무 설명";

    [SetUp]
    public void Setup()
    {
        // Create substitutes
        _kernelService = Substitute.For<IKernelService>();
        _repository = Substitute.For<IInterviewRepository>();

        // Setup valid IDs and entries
        _validResumeId = new Guid("11111111-1111-1111-1111-111111111111");
        _validJobDescriptionId = new Guid("22222222-2222-2222-2222-222222222222");

        _resumeEntry = new ResumeEntry
        {
            Id = _validResumeId,
            Content = TestResumeContent
        };

        _jobDescriptionEntry = new JobDescriptionEntry
        {
            Id = _validJobDescriptionId,
            Content = TestJobDescriptionContent,
            ResumeEntryId = _validResumeId
        };

        // Setup repository behavior
        _repository.GetResumeByIdAsync(_validResumeId).Returns(_resumeEntry);
        _repository.GetJobByIdAsync(_validJobDescriptionId).Returns(_jobDescriptionEntry);
    }

    [Test]
    public async Task PostChatCompletionAsync_WithValidData_ShouldReturnMessages()
    {
        // Arrange
        var chatRequest = new ChatRequest
        {
            ResumeId = _validResumeId,
            JobDescriptionId = _validJobDescriptionId,
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        var expectedResponse = new List<string> { "안녕하세요, 면접을 시작하겠습니다." };
        _kernelService.InvokeInterviewAgentAsync(
                Arg.Is<string>(s => s == TestResumeContent),
                Arg.Is<string>(s => s == TestJobDescriptionContent),
                Arg.Any<IEnumerable<ChatMessageContent>>())
            .Returns(expectedResponse.ToAsyncEnumerable());

        // Act
        var results = new List<ChatResponse>();
        await foreach (var response in ChatCompletionDelegate.PostChatCompletionAsync(
            chatRequest, _kernelService, _repository))
        {
            results.Add(response);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(1);
        results[0].Message.ShouldBe("안녕하세요, 면접을 시작하겠습니다.");
    }

    [Test]
    public async Task PostChatCompletionAsync_WithInvalidResumeId_ShouldReturnErrorMessage()
    {
        // Arrange
        var chatRequest = new ChatRequest
        {
            ResumeId = Guid.NewGuid(), // 유효하지 않은 이력서 ID
            JobDescriptionId = _validJobDescriptionId,
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        // Setup repository to return null for the invalid resume ID
        _repository.GetResumeByIdAsync(Arg.Is<Guid>(g => g != _validResumeId)).ReturnsNull();

        // Act
        var results = new List<ChatResponse>();
        await foreach (var response in ChatCompletionDelegate.PostChatCompletionAsync(
            chatRequest, _kernelService, _repository))
        {
            results.Add(response);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(1);
        results[0].Message.ShouldBe("이력서 또는 채용공고 데이터가 없습니다.");
    }

    [Test]
    public async Task PostChatCompletionAsync_WithInvalidJobDescriptionId_ShouldReturnErrorMessage()
    {
        // Arrange
        var chatRequest = new ChatRequest
        {
            ResumeId = _validResumeId,
            JobDescriptionId = Guid.NewGuid(), // 유효하지 않은 직무 설명 ID
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        // Setup repository to return null for the invalid job description ID
        _repository.GetJobByIdAsync(Arg.Is<Guid>(g => g != _validJobDescriptionId)).ReturnsNull();

        // Act
        var results = new List<ChatResponse>();
        await foreach (var response in ChatCompletionDelegate.PostChatCompletionAsync(
            chatRequest, _kernelService, _repository))
        {
            results.Add(response);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(1);
        results[0].Message.ShouldBe("이력서 또는 채용공고 데이터가 없습니다.");
    }

    [Test]
    public async Task PostChatCompletionAsync_WithMultipleResponses_ShouldReturnAllResponses()
    {
        // Arrange
        var chatRequest = new ChatRequest
        {
            ResumeId = _validResumeId,
            JobDescriptionId = _validJobDescriptionId,
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = MessageRoleType.User, Message = "면접을 시작합니다" }
            }
        };

        var expectedResponses = new List<string>
        {
            "안녕하세요, 면접을 시작하겠습니다.",
            "먼저 자기소개 부탁드립니다.",
            "이력서를 보니 프론트엔드 개발자 경험이 있으시네요."
        };

        _kernelService.InvokeInterviewAgentAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<ChatMessageContent>>())
            .Returns(expectedResponses.ToAsyncEnumerable());

        // Act
        var results = new List<ChatResponse>();
        await foreach (var response in ChatCompletionDelegate.PostChatCompletionAsync(
            chatRequest, _kernelService, _repository))
        {
            results.Add(response);
        }

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(3);
        results[0].Message.ShouldBe("안녕하세요, 면접을 시작하겠습니다.");
        results[1].Message.ShouldBe("먼저 자기소개 부탁드립니다.");
        results[2].Message.ShouldBe("이력서를 보니 프론트엔드 개발자 경험이 있으시네요.");
    }
}
