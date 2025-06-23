using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Get the contents of: !SQL\01.00.00.sql
// This script is used to create the database and tables for the LocalInternalAIChatBot application.
// It is assumed that the script is located in the same directory as this Program.cs file.
// If the script is located elsewhere, adjust the path accordingly.
string scriptPath = Path.Combine(AppContext.BaseDirectory, "!SQL", "01.00.00.sql");
string sqlScript = await File.ReadAllTextAsync(scriptPath);

// Define a secret parameter for SA
var saPassword = builder.AddParameter("sqlServer-password", secret: true);

// Pass that parameter into AddSqlServer
// Add a SQL Server container
var sqlServer = builder.AddSqlServer("sqlServer", saPassword)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = sqlServer.AddDatabase("LocalInternalAIChatBot")
    .WithCreationScript(sqlScript);

// Add Ollma container for AI models
var ollama = builder.AddOllama("ollama", 11434) // Ollama default port
    .WithDataVolume();
    //.WithContainerRuntimeArgs("--gpus=all");

var chat = ollama.AddModel("chat", "phi3.5");
var embeddings = ollama.AddModel("embeddings", "all-minilm");

// Add a local AI chat bot project
builder.AddProject<Projects.LocalInternalAIChatBot_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(ollama)
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(db)
    .WaitFor(chat)
    .WaitFor(embeddings)
    .WaitFor(db);

builder.Build().Run();