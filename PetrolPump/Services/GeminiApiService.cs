using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlMadinaShahzad.Services
{
    public class GeminiApiService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private int _keyIndex;

        public async Task<AiResult> AskAsync(string userQuestion, GeminiQueryMode mode, string extraContext = null)
        {
            var keys = GeminiConfig.GetAllKeys();
            if (keys.Count == 0)
                return AiResult.Skipped();

            if (!GeminiModelResolver.IsValidKeyFormat(keys[0]))
                return AiResult.Skipped();

            string prompt = AiPromptBuilder.Build(userQuestion, mode, extraContext);
            int maxTokens = AiPromptBuilder.MaxTokens(mode);

            for (int attempt = 0; attempt < keys.Count; attempt++)
            {
                string apiKey = keys[_keyIndex % keys.Count];
                _keyIndex++;

                string[] models = await GeminiModelResolver.GetModelsAsync(apiKey).ConfigureAwait(false);

                foreach (string model in models)
                {
                    try
                    {
                        string answer = await CallGeminiAsync(apiKey, prompt, model, maxTokens).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(answer))
                            return AiResult.FromApi(answer);
                    }
                    catch (RetryableApiException)
                    {
                        continue;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

            return AiResult.Skipped();
        }

        private static async Task<string> CallGeminiAsync(string apiKey, string prompt, string model, int maxOutputTokens)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.2, maxOutputTokens = maxOutputTokens }
            };

            string json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Http.PostAsync(url, content).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode == 404 || (int)response.StatusCode == 429 ||
                    responseText.Contains("RESOURCE_EXHAUSTED") || responseText.Contains("quota") ||
                    responseText.Contains("not found"))
                    throw new RetryableApiException(responseText);

                throw new RetryableApiException(responseText);
            }

            return ParseGeminiResponse(responseText);
        }

        private static string ParseGeminiResponse(string json)
        {
            var root = JObject.Parse(json);
            var text = root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();

            string block = root["candidates"]?[0]?["finishReason"]?.ToString();
            if (block == "SAFETY")
                return "Jawab block ho gaya (safety filter). Question dobara likhein.";

            return null;
        }

        private class RetryableApiException : Exception
        {
            public RetryableApiException(string msg) : base(msg) { }
        }
    }
}
