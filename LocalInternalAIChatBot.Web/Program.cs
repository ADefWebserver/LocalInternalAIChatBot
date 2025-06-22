using LocalInternalAIChatBot;
using LocalInternalAIChatBot.Web;
using LocalInternalAIChatBot.Web.Components;
using LocalInternalAIChatBot.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the ChatSQLVectorService service
builder.Services.AddScoped<ChatSQLVectorService>();

// Ollama API client configuration.
builder.AddOllamaApiClient("chat")
    .AddChatClient()
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());

// Ollama service configuration.
builder.Services.AddHttpClient<ChatService>(client =>
{
    client.BaseAddress = new("http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<EmbeddingsService>(client =>
{
    client.BaseAddress = new("http://localhost:11434");
});

// Register the DbContext with SQL Server
builder.Services.AddDbContext<LocalInternalAIChatBotContext>(opts =>
  opts.UseSqlServer(builder.Configuration.GetConnectionString("LocalInternalAIChatBot")));

// Register the Radzen
builder.Services.AddRadzenComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();