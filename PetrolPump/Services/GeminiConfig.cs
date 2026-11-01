using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AlMadinaShahzad.Services
{
    public static class GeminiConfig
    {
        private static readonly string ExtraKeysFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiselPetrolPump", "gemini_keys.txt");

        public static string Model =>
            ConfigurationManager.AppSettings["GeminiModel"] ?? "gemini-2.5-flash";

        public static List<string> GetAllKeys()
        {
            var keys = new List<string>();

            string configKeys = ConfigurationManager.AppSettings["GeminiApiKeys"] ?? "";
            foreach (string k in configKeys.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = k.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !keys.Contains(trimmed))
                    keys.Add(trimmed);
            }

            if (File.Exists(ExtraKeysFile))
            {
                foreach (string line in File.ReadAllLines(ExtraKeysFile))
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !keys.Contains(trimmed))
                        keys.Add(trimmed);
                }
            }

            return keys;
        }

        public static void AddExtraKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return;

            apiKey = apiKey.Trim();
            var existing = GetAllKeys();
            if (existing.Contains(apiKey))
                return;

            string dir = Path.GetDirectoryName(ExtraKeysFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.AppendAllText(ExtraKeysFile, apiKey + Environment.NewLine);
        }

        public static bool HasAnyKey() => GetAllKeys().Count > 0;
    }
}
