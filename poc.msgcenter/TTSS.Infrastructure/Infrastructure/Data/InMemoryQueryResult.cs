using System.Collections;

namespace TTSS.Infrastructure.Data
{
    internal class InMemoryQueryResult<T> : IQueryResult<T>
    {
        private readonly IEnumerable<T> data;

        public long TotalCount => data.Count();

        public InMemoryQueryResult(IEnumerable<T> data)
            => this.data = data;

        public Task<IEnumerable<T>> GetAsync()
            => Task.FromResult(data);

        public IPagingRepositoryResult<T> ToPaging(bool totalCount = false, int pageSize = 0)
            => new InMemoryPagingResult<T>(data, totalCount, pageSize);

        public IEnumerator<T> GetEnumerator()
            => data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => data.GetEnumerator();
    }
}
