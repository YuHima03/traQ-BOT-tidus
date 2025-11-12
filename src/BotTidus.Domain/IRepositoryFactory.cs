namespace BotTidus.Domain;

public interface IRepositoryFactory
{
    Task<IRepository> CreateRepositoryAsync(CancellationToken cancellationToken = default);
}
