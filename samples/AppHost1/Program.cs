using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<MinimalHtml_Sample>("Sample");
builder.AddDockerComposePublisher();

builder.Build().Run();
