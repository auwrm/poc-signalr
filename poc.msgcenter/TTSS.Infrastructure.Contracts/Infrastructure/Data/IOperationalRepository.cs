namespace TTSS.Infrastructure.Data
{
    public interface IOperationalRepository<T, K> : IRepositoryBase
    {
        Task InsertAsync(T data, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(K key, T data, CancellationToken cancellationToken = default);
    }
}
