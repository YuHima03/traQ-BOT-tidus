using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BotTidus.Helpers
{
    internal static class SpanHelper
    {
        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, out int charsUsed)
        {
            if (span.IsEmpty)
            {
                charsUsed = 0;
                return [];
            }

            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(span[i]))
                {
                    charsUsed = i;
                    return span[i..];
                }
            }
            charsUsed = span.Length;
            return [];
        }

        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars, out int charsTrimmed)
        {
            var trimmed = span.TrimStart(trimChars);
            charsTrimmed = trimmed.IsEmpty ? span.Length : ((int)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(trimmed)) / 2);
            return trimmed;
        }

        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, Predicate<char> predicate) => TrimStart(span, predicate, out _);

        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, Predicate<char> predicate, out int charsTrimmed)
        {
            if (span.IsEmpty)
            {
                charsTrimmed = 0;
                return [];
            }
            else if (!predicate(span[0]))
            {
                charsTrimmed = 0;
                return span;
            }

            for (int i = 1; i < span.Length; i++)
            {
                if (!predicate(span[i]))
                {
                    charsTrimmed = i;
                    return span[i..];
                }
            }
            charsTrimmed = span.Length;
            return [];
        }
    }
}
