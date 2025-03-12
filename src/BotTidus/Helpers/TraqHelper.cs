namespace BotTidus.Helpers
{
    static class TraqHelper
    {
        public static ValueTask<bool> TryGetUserIdFromNameAsync(this Traq.Api.IUserApi api, string username, out ValueTask<Traq.Model.User> resultTask, CancellationToken ct)
        {
            var task = api.GetUsersAsync(null, username, ct).ContinueWith(t => t.Result.SingleOrDefault()!);
            resultTask = new(task);
            return new(task.ContinueWith(t => t.Result is not null));
        }
    }
}
