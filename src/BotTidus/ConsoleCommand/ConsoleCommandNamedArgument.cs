namespace BotTidus.ConsoleCommand;

readonly ref struct ConsoleCommandNamedArgument
{
    public ReadOnlySpan<char> Name { get; init; }

    public ReadOnlySpan<char> Value { get; init; }
}
