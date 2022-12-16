namespace TTSS.Infrastructure.Data
{
    public interface IInsertBulkRepository<T> : IRepositoryBase
    {
        Task InsertBulkAsync(IEnumerable<T> data, CancellationToken cancellationToken = default);
    }
}
