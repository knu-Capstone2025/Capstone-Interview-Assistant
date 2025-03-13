var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.InterviewAssistant_ApiService>("apiservice");

builder.AddProject<Projects.InterviewAssistant_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
