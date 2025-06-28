// Import necessary namespaces for DTOs, HTTP operations, streaming, and AI model handling
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
    // Service class responsible for interacting with a chat API
    public class ChatService(HttpClient _httpClient)
    {
        // The name of the AI model used for chat
        private const string Model = "phi3.5";

        // Max number of retry attempts for network-related errors
        private const int MaxRetries = 3;

        // Delay in milliseconds between retry attempts
        private const int RetryDelayMilliseconds = 2000;

        /// <summary>
        /// Public method to request chat response with retry logic.
        /// Streams chunks of response back via callback delegate.
        /// </summary>
        public async Task GetChatResponseAsync(string chatprompt, Action<string> onChunk)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    // Attempt to get chat response
                    await InternalGetChatResponseAsync(chatprompt, onChunk);
                    return; // Exit loop on success
                }
                // Handle timeout errors and retry
                catch (TaskCanceledException ex) when (attempt < MaxRetries)
                {
                    Console.WriteLine(
                        $"[Retry {attempt}] Timeout occurred, retrying in {RetryDelayMilliseconds}ms...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
                // Handle transient network errors and retry
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    Console.WriteLine($"[Retry {attempt}] Network error: {ex.Message}. Retrying...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
                // Handle any other recoverable exceptions and retry
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    Console.WriteLine($"[Retry {attempt}] Unexpected error: {ex.Message}. Retrying...");
                    await Task.Delay(RetryDelayMilliseconds);
                }
                // Final failure after retries
                catch
                {
                    throw; // Rethrow the original exception
                }
            }

            // Throw error if all retries failed
            throw new Exception("Chat request failed after maximum retries.");
        }

        /// <summary>
        /// Internal method that sends the request and streams the chat response line-by-line.
        /// </summary>
        private async Task InternalGetChatResponseAsync(string chatprompt, Action<string> onChunk)
        {
            // Create the request payload for the chat API
            var request = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = chatprompt }
                },
                stream = true // Enable streaming response
            };

            // Prepare HTTP POST request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request)
            };

            // Send the request and expect a streamed response
            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            // Check for HTTP success status
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Streaming chat request failed: {response.StatusCode}");

            // Open the response stream and a StreamReader to read the data line-by-line
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            // Read and process each line from the stream
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                // Skip empty or whitespace-only lines
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // Parse each line as a JSON document
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    // Extract message content if available and send it to the callback
                    if (root.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        onChunk(content.GetString() ?? "");
                    }
                }
                catch (JsonException ex)
                {
                    // Log malformed JSON and continue
                    Console.WriteLine($"Malformed JSON line skipped: {ex.Message}");
                }
            }
        }
    }
}