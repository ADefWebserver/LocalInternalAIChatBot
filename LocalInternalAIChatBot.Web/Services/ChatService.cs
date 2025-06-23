using LocalInternalAIChatBot.Web.Models.DTO;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalInternalAIChatBot
{
    public class ChatService(HttpClient _httpClient)
    {
        private const string Model = "phi3.5";
        private const int MaxRetries = 3;
        private const int RetryDelayMilliseconds = 2000;

        public async Task GetChatResponseAsync(string chatprompt, Action<string> onChunk)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await InternalGetChatResponseAsync(chatprompt, onChunk);
                    return; // Success
                }
                catch (TaskCanceledException ex) when (attempt < MaxRetries)
                {
                    Console.WriteLine($"[Retry {attempt}] Timeout occurred, retrying in {RetryDelayMilliseconds}ms...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    Console.WriteLine($"[Retry {attempt}] Network error: {ex.Message}. Retrying...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    Console.WriteLine($"[Retry {attempt}] Unexpected error: {ex.Message}. Retrying...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
                catch
                {
                    throw; // Rethrow on final failure
                }
            }

            throw new Exception("Chat request failed after maximum retries.");
        }

        private async Task InternalGetChatResponseAsync(string chatprompt, Action<string> onChunk)
        {
            var request = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = chatprompt }
                },
                stream = true
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request)
            };

            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Streaming chat request failed: {response.StatusCode}");

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        onChunk(content.GetString() ?? "");
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Malformed JSON line skipped: {ex.Message}");
                }
            }
        }
    }
}