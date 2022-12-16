namespace TTSS.Infrastructure.Data
{
    public interface IUpsertRepository<T, K> : IOperationalRepository<T, K>
    {
        Task<bool> UpsertAsync(K key, T data, CancellationToken cancellationToken = default);
    }
}
