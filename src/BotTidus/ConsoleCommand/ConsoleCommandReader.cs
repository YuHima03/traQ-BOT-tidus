using BotTidus.Helpers;

namespace BotTidus.ConsoleCommand
{
    internal ref struct ConsoleCommandReader()
    {
        public readonly ReadOnlySpan<char> CommandName { get; }

        public bool EnumeratedAll
        {
            get
            {
                var index = _index;
                var args = _rawArguments;
                if (index == args.Length)
                {
                    return true;
                }
                else if (args[index..].TrimStart(out var leadingSpaces).IsEmpty)
                {
                    _index = args.Length;
                    return true;
                }
                else
                {
                    _index = index + leadingSpaces;
                    return false;
                }
            }
        }

        public readonly bool HasAnyArguments => !_rawArguments.IsEmpty;

        public readonly bool IsDevelopingCommand { get; }

        public readonly bool IsMentioning { get; }

        readonly ReadOnlySpan<char> _rawArguments;
        int _index = 0;

        private ConsoleCommandReader(ReadOnlySpan<char> commandName, ReadOnlySpan<char> args, bool isMentioning = false) : this()
        {
            IsDevelopingCommand = !commandName.IsEmpty && commandName[0] == '_';
            IsMentioning = isMentioning;
            CommandName = IsDevelopingCommand ? commandName[1..] : commandName;
            _rawArguments = args.Trim();
        }

        public static bool TryCreate(ReadOnlySpan<char> command, bool isMentioning, ReadOnlySpan<char> prefix, out ConsoleCommandReader reader)
        {
            command = command.Trim();
            if (command.IsEmpty || !command.StartsWith(prefix))
            {
                if (isMentioning)
                {
                    reader = new([], command, true);
                    return true;
                }
                reader = default;
                return false;
            }
            command = command[prefix.Length..];

            for (int i = 0; i < command.Length; i++)
            {
                if (char.IsWhiteSpace(command[i]))
                {
                    reader = new(command[..i], command[i..].Trim(), isMentioning);
                    return true;
                }
            }
            reader = new(command, [], isMentioning);
            return true;
        }

        public bool NextArgumentNameOnly(out ReadOnlySpan<char> name)
        {
            var index = _index;
            var charsUsed = tryGetName(_rawArguments[index..], out name);

            _index = index + charsUsed;
            return charsUsed != 0;

            static int tryGetName(ReadOnlySpan<char> s, out ReadOnlySpan<char> name)
            {
                if (s.IsEmpty)
                {
                    name = default;
                    return 0;
                }

                s = s.TrimStart(out var leadingSpaces);
                if (s.IsEmpty || s[0] != '-')
                {
                    name = default;
                    return leadingSpaces;
                }

                for (int i = 1; i < s.Length; i++)
                {
                    if (char.IsWhiteSpace(s[i]))
                    {
                        name = s[..i];
                        return leadingSpaces + i;
                    }
                }

                name = s;
                return s.Length;
            }
        }

        public bool NextNamedArgument(out ConsoleCommandNamedArgument value)
        {
            var index = _index;

            if (NextArgumentNameOnly(out var name))
            {
                var c0 = _index - index;
                if (NextValueOnly(out var valExp))
                {
                    value = new() { Name = name, Value = valExp };
                    return true;
                }
                _index -= c0;
            }
            value = default;
            return false;
        }

        public bool NextValueOnly(out ReadOnlySpan<char> value)
        {
            var index = _index;
            var charsUsed = tryGetValue(_rawArguments[index..], out value);

            _index = index + charsUsed;
            return charsUsed != 0;

            static int tryGetValue(ReadOnlySpan<char> s, out ReadOnlySpan<char> value)
            {
                if (s.IsEmpty)
                {
                    value = default;
                    return 0;
                }

                int leadingSpaces = 0;
                int valueLength = 0;

                bool beginsWithQuotationMark = false;

                using var en = new Traq.Extensions.Messages.MessageElementEnumerator(s);

                _ = en.MoveNext();
                var e = en.Current;
                if (e.Kind == Traq.Extensions.Messages.MessageElementKind.NormalText)
                {
                    var text = e.GetText().TrimStart(out leadingSpaces);
                    if (!text.IsEmpty)
                    {
                        valueLength += text.Length;
                        beginsWithQuotationMark = text[0] == '"';
                    }
                }
                else
                {
                    valueLength += e.RawText.Length;
                }

                if (beginsWithQuotationMark)
                {
                    while (en.MoveNext())
                    {
                        e = en.Current;
                        if (e.Kind == Traq.Extensions.Messages.MessageElementKind.NormalText)
                        {
                            var text = e.GetText();
                            var escaping = false;
                            for (int i = 0; i < text.Length; i++)
                            {
                                if (escaping)
                                {
                                    escaping = false;
                                }
                                else if (text[i] == '\\')
                                {
                                    escaping = true;
                                }
                                else if (text[i] == '"')
                                {
                                    valueLength += i;
                                    value = s.Slice(leadingSpaces + 1, valueLength - 1);
                                    return leadingSpaces + valueLength + 1;
                                }
                            }
                        }
                        valueLength += e.RawText.Length;
                    }
                }
                else
                {
                    while (en.MoveNext())
                    {
                        e = en.Current;
                        if (e.Kind == Traq.Extensions.Messages.MessageElementKind.NormalText)
                        {
                            var text = e.GetText();
                            for (int i = 0; i < text.Length; i++)
                            {
                                if (char.IsWhiteSpace(text[i]))
                                {
                                    valueLength += i;
                                    value = s.Slice(leadingSpaces, valueLength);
                                    return leadingSpaces + i;
                                }
                            }
                        }
                        valueLength += e.RawText.Length;
                    }
                }

                if (beginsWithQuotationMark)
                {
                    value = default;
                    return 0;
                }
                else
                {
                    value = s[leadingSpaces..];
                    return s.Length;
                }
            }
        }

        public bool NextArgument(out ConsoleCommandArgument argument)
        {
            if (NextNamedArgument(out var namedArg))
            {
                argument = ConsoleCommandArgument.Create(namedArg.Name, namedArg.Value);
                return true;
            }
            else if (NextValueOnly(out var value))
            {
                argument = ConsoleCommandArgument.CreateValueOnly(value);
                return true;
            }
            else if (NextArgumentNameOnly(out var name))
            {
                argument = ConsoleCommandArgument.CreateNameOnly(name);
                return true;
            }
            else
            {
                argument = default;
                return false;
            }
        }
    }

    public readonly ref struct ConsoleCommandArgument
    {
        public bool HasName { get; init; }
        public bool HasValue { get; init; }
        public ReadOnlySpan<char> Name { get; init; }
        public ReadOnlySpan<char> Value { get; init; }

        public static ConsoleCommandArgument Create(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            return new() { HasName = true, HasValue = true, Name = name, Value = value };
        }

        public static ConsoleCommandArgument CreateNameOnly(ReadOnlySpan<char> name)
        {
            return new() { HasName = true, Name = name };
        }

        public static ConsoleCommandArgument CreateValueOnly(ReadOnlySpan<char> value)
        {
            return new() { HasValue = true, Value = value };
        }
    }
}
