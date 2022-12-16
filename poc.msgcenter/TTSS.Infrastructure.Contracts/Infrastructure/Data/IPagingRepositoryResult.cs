using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data
{
    public interface IPagingRepositoryResult<T>
    {
        PagingResult<T> GetPage(int pageNo);
        Task<IEnumerable<T>> GetDataAsync(int pageNo);
        IPagingRepositoryResult<T> OrderBy(Expression<Func<T, object>> keySelector);
        IPagingRepositoryResult<T> OrderByDescending(Expression<Func<T, object>> keySelector);
        IPagingRepositoryResult<T> ThenBy(Expression<Func<T, object>> keySelector);
        IPagingRepositoryResult<T> ThenByDescending(Expression<Func<T, object>> keySelector);
    }
}
