namespace BotTidus.ConsoleCommand
{
    internal readonly ref struct ConsoleCommandNamedArgument
    {
        public ReadOnlySpan<char> Name { get; init; }

        public ReadOnlySpan<char> Value { get; init; }
    }
}
