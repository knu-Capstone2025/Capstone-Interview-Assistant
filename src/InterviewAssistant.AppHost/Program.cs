var builder = DistributedApplication.CreateBuilder(args);

var mcpserver = builder.AddDockerfile("mcpserver", "../InterviewAssistant.McpMarkItDown/packages/markitdown-mcp")
                       .WithImageTag("latest")
                       .WithHttpEndpoint(3001, 3001)
                       .WithArgs("--sse", "--host", "0.0.0.0", "--port", "3001");
                       


var insights = builder.ExecutionContext.IsPublishMode
               ? builder.AddAzureApplicationInsights("applicationinsights")
               : builder.AddConnectionString("applicationinsights");

var openai = builder.AddConnectionString("openai");
var config = builder.Configuration;

var apiService = builder.AddProject<Projects.InterviewAssistant_ApiService>("apiservice")
                    .WithReference(openai)
                    .WithReference(insights)
                    .WithReference(mcpserver.GetEndpoint("http"))
                    .WaitFor(mcpserver)
                    .WaitFor(insights)
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!);

builder.AddProject<Projects.InterviewAssistant_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(insights)
    .WaitFor(insights)
    .WaitFor(apiService);

builder.Build().Run();
