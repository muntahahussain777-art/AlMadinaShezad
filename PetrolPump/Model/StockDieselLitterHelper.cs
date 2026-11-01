using System;
using System.Globalization;

namespace AlMadinaShahzad.Model
{
    internal static class StockDieselLitterHelper
    {
        public const string MinusNoteTag = "[MINUS]";

        public static bool IsMinusEntry(string note, decimal litter)
        {
            if (litter < 0) return true;
            string n = (note ?? "").Trim();
            return n.StartsWith(MinusNoteTag, StringComparison.OrdinalIgnoreCase);
        }

        public static decimal GetSignedLitter(decimal litter, string note)
        {
            decimal abs = Math.Abs(litter);
            return IsMinusEntry(note, litter) ? -abs : abs;
        }

        public static decimal GetDisplayLitter(decimal litter, string note)
        {
            return Math.Abs(litter);
        }

        public static string GetEntryType(decimal litter, string note)
        {
            return IsMinusEntry(note, litter) ? "Minus / Sale" : "Add";
        }

        public static string BuildNoteForSave(bool isMinusEntry, string userNote)
        {
            string note = StripMinusTag(userNote)?.Trim() ?? "";
            if (isMinusEntry)
                return string.IsNullOrEmpty(note) ? MinusNoteTag : MinusNoteTag + " " + note;
            return note;
        }

        public static string StripMinusTag(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return "";
            string n = note.Trim();
            if (n.StartsWith(MinusNoteTag, StringComparison.OrdinalIgnoreCase))
                return n.Substring(MinusNoteTag.Length).TrimStart();
            return n;
        }

        public static string FormatLitter(decimal value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N0}", Math.Abs(value));
        }
    }
}
