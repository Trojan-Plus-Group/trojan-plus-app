// SPDX-License-Identifier: GPL-3.0-or-later
namespace TrojanPlusApp.Services
{
    public static class LogClipboard
    {
        public const int CharacterLimit = 65536;

        public static string Tail(string text)
        {
            if (text.Length <= CharacterLimit)
            {
                return text;
            }

            int start = text.Length - CharacterLimit;
            if (char.IsLowSurrogate(text[start]) && char.IsHighSurrogate(text[start - 1]))
            {
                start++;
            }

            // Prefer complete lines, but retain a readable tail for a single oversized line.
            if (text[start - 1] != '\n')
            {
                int newline = text.IndexOf('\n', start);
                if (newline >= start && newline < text.Length - 1)
                {
                    start = newline + 1;
                }
            }

            return text.Substring(start);
        }
    }
}
