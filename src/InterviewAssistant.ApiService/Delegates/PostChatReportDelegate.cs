using InterviewAssistant.Common.Models;
using InterviewAssistant.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InterviewAssistant.ApiService.Delegates;

public static class ChatReportDelegate
{
  public static async Task<IResult> PostChatReportAsync(
      [FromBody] List<ChatMessage> messages,
      IKernelService kernelService)
  {
    var report = await kernelService.GenerateReportAsync(messages);
    return Results.Ok(report);
  }
}