using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data
{
    public interface IDeletableRepository<T, K> : IRepositoryBase
    {
        Task<bool> DeleteAsync(K key, CancellationToken cancellationToken = default);
        Task<bool> DeleteManyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    }
}
