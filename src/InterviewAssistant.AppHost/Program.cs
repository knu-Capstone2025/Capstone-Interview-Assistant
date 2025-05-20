var builder = DistributedApplication.CreateBuilder(args);

var container = builder.AddDockerfile("markitdown-mcp", "../InterviewAssistant.McpMarkItDown/packages/markitdown-mcp")
                       .WithEndpoint(3001, 3001)
                       .WithArgs("--sse", "--host", "0.0.0.0", "--port", "3001");


var insights = builder.ExecutionContext.IsPublishMode
               ? builder.AddAzureApplicationInsights("applicationinsights")
               : builder.AddConnectionString("applicationinsights");

var openai = builder.AddConnectionString("openai");
var config = builder.Configuration;

var apiService = builder.AddProject<Projects.InterviewAssistant_ApiService>("apiservice")
                    .WithReference(openai)
                    .WithReference(insights)
                    .WaitFor(insights)
                    .WithEnvironment("SemanticKernel__ServiceId", config["SemanticKernel:ServiceId"]!)
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!)
                    .WithEnvironment("services__markitdown-mcp__http__0", config["services:markitdown-mcp:http:0"]!)
                    .WithEnvironment("services__markitdown-mcp__https__0", config["services:markitdown-mcp:https:0"]!);

builder.AddProject<Projects.InterviewAssistant_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(insights)
    .WaitFor(insights)
    .WaitFor(apiService);

builder.Build().Run();
