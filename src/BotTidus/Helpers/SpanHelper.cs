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
    }
}
