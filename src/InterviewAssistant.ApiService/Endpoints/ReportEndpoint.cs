using InterviewAssistant.ApiService.Services;
using InterviewAssistant.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace InterviewAssistant.ApiService.Endpoints;

public static class ReportEndpoint
{
    public static IEndpointRouteBuilder MapReportEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        var api = routeBuilder.MapGroup("api/report").WithTags("Report");

        api.MapPost("generate-pdf", GeneratePdfReport)
           .Accepts<InterviewReport>(contentType: "application/json")
           .Produces<FileContentResult>(statusCode: StatusCodes.Status200OK, contentType: "application/pdf")
           .WithName("GeneratePdfReport")
           .WithOpenApi();

        return routeBuilder;
    }

    private static IResult GeneratePdfReport(
        [FromBody] InterviewReport report,
        IPdfReportService pdfService)
    {
        try
        {
            var pdfBytes = pdfService.GenerateInterviewReport(report);
            var fileName = $"면접리포트_{report.CandidateName}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return Results.File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "PDF 생성 오류",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
