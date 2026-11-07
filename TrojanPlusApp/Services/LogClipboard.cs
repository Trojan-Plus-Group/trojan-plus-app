// SPDX-License-Identifier: GPL-3.0-or-later
namespace TrojanPlusApp.Services
{
    using System;
    using System.Threading.Tasks;

    public static class LogClipboard
    {
        // Approximate Binder budget, not a guaranteed clipboard limit; reserve parcel overhead.
        public const int CharacterLimit = (((1024 * 1024 * 4) / 5) - (32 * 1024)) / 2;

        public static string Tail(string text, int limit)
        {
            if (limit < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            if (text.Length <= limit)
            {
                return text;
            }

            int start = text.Length - limit;
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

        public static async Task<bool> CopyAsync(string text, bool android, Func<string, Task> copy)
        {
            string candidate = android ? Tail(text, CharacterLimit) : text;
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    await copy(candidate);
                    return candidate.Length != text.Length;
                }
                catch (Exception ex) when (android && attempt < 2 && candidate.Length > 2 && IsTransactionSizeFailure(ex))
                {
                    candidate = Tail(candidate, Math.Max(2, candidate.Length / 2));
                }
            }
        }

        private static bool IsTransactionSizeFailure(Exception exception)
        {
            // Shared assembly cannot reference Android types; match type identity, never exception text.
            return exception.GetType().FullName == "Android.OS.TransactionTooLargeException"
                || (exception.InnerException != null && IsTransactionSizeFailure(exception.InnerException));
        }
    }
}
