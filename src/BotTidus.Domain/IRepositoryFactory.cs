namespace BotTidus.Domain
{
    public interface IRepositoryFactory
    {
        public Task<IRepository> CreateRepositoryAsync(CancellationToken cancellationToken = default);
    }
}
