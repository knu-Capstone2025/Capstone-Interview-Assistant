var builder = DistributedApplication.CreateBuilder(args);

var insights = builder.ExecutionContext.IsPublishMode
               ? builder.AddAzureApplicationInsights("applicationinsights")
               : builder.AddConnectionString("applicationinsights");

var openai = builder.AddConnectionString("openai");
var config = builder.Configuration;

var apiServiceBuilder = builder.AddProject<Projects.InterviewAssistant_ApiService>("apiservice")
                    .WithReference(openai)
                    .WithReference(insights)
                    .WaitFor(insights)
                    .WithEnvironment("SemanticKernel__ServiceId", config["SemanticKernel:ServiceId"]!)
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!);

var webFrontendBuilder = builder.AddProject<Projects.InterviewAssistant_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiServiceBuilder)
    .WithReference(insights)
    .WaitFor(insights)
    .WaitFor(apiServiceBuilder);

builder.Build().Run();
