namespace BotTidus.Services.InteractiveBot
{
    static class MessageReactions
    {
        public static bool TryGetReaction(ReadOnlySpan<char> s, Guid sender, out Reaction reaction)
        {
            s = s.TrimEnd(".。、,～~ー-！!").TrimEnd();

            if (s.EndsWith("しにたい") || s.EndsWith("死にたい") || s.EndsWith("ﾀﾋにたい"))
            {
                if (sender == Constants.TraqUsers.Tidus.Id && Random.Shared.Next(10) == 0)
                {
                    reaction = new Reaction($":{Constants.TraqStamps.OmaeoKorosu.Name}:", null);
                }
                else
                {
                    reaction = new Reaction("しぬな！", null);
                }
                return true;
            }
            else if ((s.EndsWith("どね") && !s.EndsWith("けどね") && !s.EndsWith("などね"))
                || s.EndsWith(":done:"))
            {
                reaction = new Reaction(null, Constants.TraqStamps.Clap.Id);
                return true;
            }
            else if (s.EndsWith("おわ"))
            {
                reaction = new Reaction(null, Constants.TraqStamps.Otsukare.Id);
                return true;
            }

            reaction = default;
            return false;
        }

        public readonly record struct Reaction(
            string? Message,
            Guid? Stamp
            );
    }
}
