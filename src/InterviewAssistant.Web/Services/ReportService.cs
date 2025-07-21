using InterviewAssistant.Common.Models;

namespace InterviewAssistant.Web.Services;

public interface IReportService
{
    Task<byte[]> GeneratePdfReportAsync(InterviewReport report);
}

public class ReportService(HttpClient httpClient) : IReportService
{
    public async Task<byte[]> GeneratePdfReportAsync(InterviewReport report)
    {
        var response = await httpClient.PostAsJsonAsync("/api/report/generate-pdf", report);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsByteArrayAsync();
    }
}
