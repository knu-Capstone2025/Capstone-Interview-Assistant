var builder = DistributedApplication.CreateBuilder(args);

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
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!);

builder.AddProject<Projects.InterviewAssistant_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(insights)
    .WaitFor(insights)
    .WaitFor(apiService);

builder.Build().Run();
