using OllamaSharp;
using System.Text.Json;

namespace LocalInternalAIChatBot
{
    /// <summary>
    /// A service class responsible for generating vector embeddings from a given text prompt
    /// by making an HTTP request to an Ollama-based embedding endpoint.
    /// </summary>
    public class EmbeddingsService(HttpClient _httpClient)
    {
        /// <summary>
        /// Asynchronously retrieves an embedding vector for the provided prompt text.
        /// </summary>
        /// <param name="prompt">The input text for which the embedding should be generated.</param>
        /// <returns>A float array representing the embedding vector.</returns>
        /// <exception cref="Exception">Thrown if the HTTP request fails 
        /// or the expected JSON structure is not returned.</exception>
        public async Task<float[]> GetEmbeddingAsync(string prompt)
        {
            // Specify the name of the model used to generate embeddings
            string _model = "all-minilm";

            // Construct an anonymous object representing the JSON body of the POST request.
            // The server expects a model identifier and a prompt string to generate embeddings.
            var request = new
            {
                model = _model,
                prompt = prompt
            };

            // Make an asynchronous HTTP POST request to the "/api/embeddings" endpoint,
            // sending the request object as JSON.
            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);

            // If the HTTP response status is not successful (200 OK, etc.), throw an error
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Embedding request failed: {response.StatusCode}");

            // Read the response content as a stream to allow efficient parsing without full buffering
            using var contentStream = await response.Content.ReadAsStreamAsync();

            // Parse the JSON content from the response stream into a JsonDocument
            using var jsonDoc = await JsonDocument.ParseAsync(contentStream);

            // Access the root of the parsed JSON document
            var root = jsonDoc.RootElement;

            // Try to extract the "embedding" property from the root JSON object.
            // If the property doesn't exist, the expected response structure is invalid.
            if (!root.TryGetProperty("embedding", out var embeddingElement))
                throw new Exception("Missing 'embedding' in response.");

            // The "embedding" field should be an array of numbers.
            // Convert each JSON number to a float using .GetSingle(), and return as a float[]
            return embeddingElement.EnumerateArray().Select(e => e.GetSingle()).ToArray();
        }
    }
}