using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 기존 기본 서비스 등록
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger 서비스 등록
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Interview Assistant API", Version = "v1" });
});

// Semantic Kernel 서비스 등록
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var azureOpenAIKey = configuration["AzureOpenAI:ApiKey"] 
        ?? throw new InvalidOperationException("Azure OpenAI API 키가 구성되지 않았습니다.");
    var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"] 
        ?? throw new InvalidOperationException("Azure OpenAI 엔드포인트가 구성되지 않았습니다.");
    var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

    // 최신 API 방식으로 커널 생성
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: azureOpenAIEndpoint,
        apiKey: azureOpenAIKey);
    
    return kernelBuilder.Build();
});

// CORS 설정 추가
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ServiceDefaults 추가 (.NET Aspire 프로젝트에 포함된 기본 설정)
builder.AddServiceDefaults();

var app = builder.Build();

// Swagger 미들웨어 추가
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Interview Assistant API v1");
    });
}

// CORS 미들웨어 활성화
app.UseCors("AllowAll");

// 기타 미들웨어 설정
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ServiceDefaults 맵핑 (.NET Aspire 프로젝트에 포함된 기본 설정)
app.MapDefaultEndpoints();

app.Run();