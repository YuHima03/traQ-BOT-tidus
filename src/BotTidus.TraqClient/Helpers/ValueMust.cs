using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BotTidus.TraqClient.Helpers
{
    internal static class ValueMust
    {
        public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(value, paramName);
            return value;
        }
    }
}
