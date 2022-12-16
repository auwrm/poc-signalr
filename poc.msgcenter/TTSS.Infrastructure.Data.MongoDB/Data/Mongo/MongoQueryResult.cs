using MongoDB.Driver;
using System.Collections;

namespace TTSS.Infrastructure.Data.Mongo
{
    internal class MongoQueryResult<T> : IQueryResult<T>
    {
        private readonly IFindFluent<T, T> findResult;
        private readonly CancellationToken cancellationToken;

        public long TotalCount => findResult.CountDocuments(cancellationToken);

        public MongoQueryResult(IFindFluent<T, T> findResult, CancellationToken cancellationToken)
        {
            this.findResult = findResult ?? throw new ArgumentNullException(nameof(findResult));
            this.cancellationToken = cancellationToken;
        }

        public async Task<IEnumerable<T>> GetAsync()
            => await findResult.ToListAsync(cancellationToken);

        public IPagingRepositoryResult<T> ToPaging(bool totalCount = false, int pageSize = 0)
            => new MongoPagingResult<T>(findResult, cancellationToken, totalCount, pageSize);

        public IEnumerator<T> GetEnumerator()
            => findResult.ToEnumerable(cancellationToken).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => findResult.ToEnumerable(cancellationToken).GetEnumerator();
    }
}
