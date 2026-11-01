using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AlMadinaShahzad.Services
{
    public static class GeminiModelResolver
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private static List<string> _cachedModels;
        private static DateTime _cacheTime = DateTime.MinValue;

        private static readonly string[] FallbackModels =
        {
            "gemini-2.5-flash",
            "gemini-2.5-flash-lite",
            "gemini-2.0-flash-001",
            "gemini-2.0-flash-lite-001",
            "gemini-2.0-flash",
            "gemini-2.0-flash-lite"
        };

        public static async Task<string[]> GetModelsAsync(string apiKey)
        {
            var result = new List<string>();

            string preferred = GeminiConfig.Model;
            if (!string.IsNullOrEmpty(preferred))
                result.Add(preferred);

            if (_cachedModels != null && (DateTime.UtcNow - _cacheTime).TotalMinutes < 30)
            {
                foreach (string m in _cachedModels)
                    if (!result.Contains(m)) result.Add(m);
            }
            else if (!string.IsNullOrEmpty(apiKey))
            {
                var discovered = await FetchFromApiAsync(apiKey).ConfigureAwait(false);
                if (discovered.Count > 0)
                {
                    _cachedModels = discovered;
                    _cacheTime = DateTime.UtcNow;
                    foreach (string m in discovered)
                        if (!result.Contains(m)) result.Add(m);
                }
            }

            foreach (string m in FallbackModels)
                if (!result.Contains(m)) result.Add(m);

            return result.ToArray();
        }

        public static string[] GetModelsSync()
        {
            var result = new List<string>();
            string preferred = GeminiConfig.Model;
            if (!string.IsNullOrEmpty(preferred))
                result.Add(preferred);

            if (_cachedModels != null)
                foreach (string m in _cachedModels)
                    if (!result.Contains(m)) result.Add(m);

            foreach (string m in FallbackModels)
                if (!result.Contains(m)) result.Add(m);

            return result.ToArray();
        }

        private static async Task<List<string>> FetchFromApiAsync(string apiKey)
        {
            var models = new List<string>();
            try
            {
                string url = "https://generativelanguage.googleapis.com/v1beta/models?key=" +
                             Uri.EscapeDataString(apiKey);
                string json = await Http.GetStringAsync(url).ConfigureAwait(false);
                var root = JObject.Parse(json);

                foreach (var item in root["models"] ?? new JArray())
                {
                    string name = item["name"]?.ToString() ?? "";
                    if (!name.StartsWith("models/")) continue;

                    var methods = item["supportedGenerationMethods"] as JArray;
                    bool supportsGenerate = methods != null &&
                        methods.Any(m => m.ToString().Equals("generateContent", StringComparison.OrdinalIgnoreCase));

                    if (!supportsGenerate) continue;

                    string modelId = name.Substring("models/".Length);
                    if (modelId.Contains("gemini") && !modelId.Contains("embedding") && !modelId.Contains("aqa"))
                        models.Add(modelId);
                }
            }
            catch { }

            return models;
        }

        public static bool IsValidKeyFormat(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            key = key.Trim();
            return key.StartsWith("AIza", StringComparison.OrdinalIgnoreCase);
        }
    }
}
