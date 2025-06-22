using OllamaSharp;
using OllamaSharp.Models;
using System.Text.Json;

namespace LocalInternalAIChatBot
{
    public class EmbeddingsService(HttpClient _httpClient)
    {
        public async Task<float[]> GetEmbeddingAsync(string prompt)
        {
            string _model = "all-minilm";

            var request = new
            {
                model = _model,
                prompt = prompt
            };

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Embedding request failed: {response.StatusCode}");

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var jsonDoc = await JsonDocument.ParseAsync(contentStream);

            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("embedding", out var embeddingElement))
                throw new Exception("Missing 'embedding' in response.");

            return embeddingElement.EnumerateArray().Select(e => e.GetSingle()).ToArray();
        }
    }
}