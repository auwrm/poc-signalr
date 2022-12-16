namespace TTSS.Infrastructure.Data
{
    public interface IQueryResult<T> : IEnumerable<T>
    {
        long TotalCount { get; }

        Task<IEnumerable<T>> GetAsync();
        IPagingRepositoryResult<T> ToPaging(bool totalCount = false, int pageSize = 0);
    }
}
