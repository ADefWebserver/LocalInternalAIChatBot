using LocalInternalAIChatBot.Web;
using LocalInternalAIChatBot.Web.Components;
using LocalInternalAIChatBot.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Ollama API client configuration.
builder.AddOllamaApiClient("chat")
    .AddChatClient()
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());

// Ollama embeddings service configuration.
builder.Services.AddHttpClient<EmbeddingsService>(client =>
{
    client.BaseAddress = new("http://localhost:11434");
});

builder.Services.AddDbContext<LocalInternalAIChatBotContext>(opts =>
  opts.UseSqlServer(builder.Configuration.GetConnectionString("dbConnectionString")));

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