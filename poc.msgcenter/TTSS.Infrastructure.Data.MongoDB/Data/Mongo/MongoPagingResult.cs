using MongoDB.Driver;
using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data.Mongo
{
    internal class MongoPagingResult<T> : IPagingRepositoryResult<T>
    {
        private IFindFluent<T, T> findResult;
        private readonly CancellationToken cancellationToken;
        private readonly int pageSize;
        private readonly int totalCount;

        public MongoPagingResult(IFindFluent<T, T> findResult, CancellationToken cancellationToken, bool totalCount = false, int pageSize = 0)
        {
            this.findResult = findResult;
            this.cancellationToken = cancellationToken;
            this.pageSize = pageSize;
            this.totalCount = totalCount ? (int)this.findResult.CountDocuments(cancellationToken) : 0;
        }

        public PagingResult<T> GetPage(int pageNo)
            => new PagingResult<T>(getPageDataInternal(pageNo), pageSize, pageNo, () => totalCount);

        public Task<IEnumerable<T>> GetDataAsync(int pageNo)
            => getPageDataInternal(pageNo);

        public IPagingRepositoryResult<T> OrderBy(Expression<Func<T, object>> keySelector)
        {
            findResult = findResult.SortBy(keySelector);
            return this;
        }

        public IPagingRepositoryResult<T> OrderByDescending(Expression<Func<T, object>> keySelector)
        {
            findResult = findResult.SortByDescending(keySelector);
            return this;
        }

        public IPagingRepositoryResult<T> ThenBy(Expression<Func<T, object>> keySelector)
        {
            findResult = findResult.SortBy(keySelector);
            return this;
        }

        public IPagingRepositoryResult<T> ThenByDescending(Expression<Func<T, object>> keySelector)
        {
            findResult = findResult.SortByDescending(keySelector);
            return this;
        }

        private async Task<IEnumerable<T>> getPageDataInternal(int pageNo)
            => await findResult.Skip(pageNo * pageSize).Limit(pageSize).ToListAsync(cancellationToken);
    }
}
