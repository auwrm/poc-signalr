using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data
{
    public interface IQueryRepository<T, K> : IRepositoryBase
    {
        Task<T> GetByIdAsync(K key, CancellationToken cancellationToken = default);
        IEnumerable<T> Get(CancellationToken cancellationToken = default);
        IEnumerable<T> Get(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    }
}
