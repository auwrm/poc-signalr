namespace TTSS.Infrastructure.Data
{
    public static class QueryResultExtensions
    {
        public static Task<IEnumerable<T>> GetAsync<T>(this IEnumerable<T> result)
        {
            if (result is IQueryResult<T> qresult)
            {
                return qresult.GetAsync();
            }

            return Task.FromResult(result);
        }

        public static IPagingRepositoryResult<T> ToPaging<T>(this IEnumerable<T> result, bool totalCount = false, int pageSize = 0)
        {
            if (result is IQueryResult<T> qresult)
            {
                return qresult.ToPaging(totalCount, pageSize);
            }

            throw new NotSupportedException("The underlying result is not paging-enabled.");
        }
    }
}
