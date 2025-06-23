using LocalInternalAIChatBot.Web.Models.DTO;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalInternalAIChatBot
{
    public class ChatService(HttpClient _httpClient)
    {
        private const string Model = "phi3.5";

        public async Task GetChatResponseAsync(string chatprompt, Action<string> onChunk)
        {
            var request = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = chatprompt }
                },
                stream = true,
                options = new
                {
                    temperature = 0.7,
                    num_ctx = 2048
                }
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