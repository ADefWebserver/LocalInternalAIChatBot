using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var dbConnectionString = builder.AddConnectionString("DefaultConnection");

var ollama = builder.AddOllama("ollama", 11434)
    .WithDataVolume()
    .WithContainerRuntimeArgs("--gpus=all");

var chat = ollama.AddModel("chat", "phi3.5");
var embeddings = ollama.AddModel("embeddings", "all-minilm");

builder.AddProject<Projects.LocalInternalAIChatBot_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(ollama)
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(dbConnectionString) // Use the correctly casted reference
    .WaitFor(chat)
    .WaitFor(embeddings);

builder.Build().Run();