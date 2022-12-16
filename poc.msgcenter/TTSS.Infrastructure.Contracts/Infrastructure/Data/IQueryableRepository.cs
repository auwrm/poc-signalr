namespace TTSS.Infrastructure.Data
{
    public interface IQueryableRepository<T> : IRepositoryBase
    {
        IQueryable<T> Query(CancellationToken cancellationToken = default);
    }
}
