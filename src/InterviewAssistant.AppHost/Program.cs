var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddConnectionString("openai");

var config = builder.Configuration;

var apiService = builder.AddProject<Projects.InterviewAssistant_ApiService>("apiservice")
                    .WithReference(openai)
                    .WithEnvironment("SemanticKernel__ServiceId", config["SemanticKernel:ServiceId"]!)
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!);


builder.AddProject<Projects.InterviewAssistant_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
