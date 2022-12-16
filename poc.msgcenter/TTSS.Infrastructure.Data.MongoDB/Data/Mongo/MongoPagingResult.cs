using MongoDB.Driver;
using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data.Mongo
{
    internal class MongoPagingResult<T> : IPagingRepositoryResult<T>
    {
        private IFindFluent<T, T> findResult;
        private readonly CancellationToken cancellationToken;
        private readonly int pageSize;
        private readonly int totalDocumentCount;

        public MongoPagingResult(IFindFluent<T, T> findResult, CancellationToken cancellationToken, bool totalCount = false, int pageSize = 0)
        {
            this.findResult = findResult;
            this.cancellationToken = cancellationToken;
            this.pageSize = pageSize;
            this.totalDocumentCount = totalCount ? (int)this.findResult.CountDocuments(cancellationToken) : 0;
        }

        public PagingResult<T> GetPage(int pageNo)
        {
            int page = pageNo;
            var data = getPageDataInternal(pageNo);

            return new PagingResult<T>(data, pageSize, 0, () => totalDocumentCount);
        }

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
