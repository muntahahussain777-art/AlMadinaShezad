using System;

namespace AlMadinaShahzad.Services
{
    public enum GeminiQueryMode
    {
        General,
        Software,
        Database
    }

    public static class GeminiQueryParser
    {
        public static (GeminiQueryMode mode, string question) Parse(string raw)
        {
            string q = (raw ?? "").Trim();
            if (q.Length == 0)
                return (GeminiQueryMode.General, "");

            if (StartsWithCommand(q, "/software"))
                return (GeminiQueryMode.Software, q.Substring("/software".Length).Trim());

            if (StartsWithCommand(q, "/database"))
                return (GeminiQueryMode.Database, q.Substring("/database".Length).Trim());

            return (GeminiQueryMode.General, q);
        }

        private static bool StartsWithCommand(string text, string command)
        {
            if (!text.StartsWith(command, StringComparison.OrdinalIgnoreCase))
                return false;
            if (text.Length == command.Length)
                return true;
            char next = text[command.Length];
            return next == ' ' || next == '\t';
        }

        public static string ModeLabel(GeminiQueryMode mode)
        {
            switch (mode)
            {
                case GeminiQueryMode.Software: return "Software Guide";
                case GeminiQueryMode.Database: return "Database";
                default: return "General";
            }
        }
    }
}
