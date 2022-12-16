using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data
{
    internal class InMemoryPagingResult<T> : IPagingRepositoryResult<T>
    {
        private IEnumerable<T> data;
        private readonly int pageSize;
        private readonly int totalCount;

        internal InMemoryPagingResult(IEnumerable<T> data, bool totalCount = false, int pageSize = 0)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.pageSize = pageSize;
            this.totalCount = totalCount ? data.Count() : 0;
        }

        public PagingResult<T> GetPage(int pageNo)
            => new PagingResult<T>(Task.FromResult(getPageDataInternal(pageNo)), pageSize, pageNo, () => totalCount);

        public Task<IEnumerable<T>> GetDataAsync(int pageNo)
            => Task.FromResult(getPageDataInternal(pageNo));

        public IPagingRepositoryResult<T> OrderBy(Expression<Func<T, object>> keySelector)
        {
            data = data.OrderBy(keySelector.Compile());
            return this;
        }

        public IPagingRepositoryResult<T> OrderByDescending(Expression<Func<T, object>> keySelector)
        {
            data = data.OrderByDescending(keySelector.Compile());
            return this;
        }

        public IPagingRepositoryResult<T> ThenBy(Expression<Func<T, object>> keySelector)
        {
            data = data is IOrderedEnumerable<T> orderedData ? orderedData.ThenBy(keySelector.Compile()) : data.OrderBy(keySelector.Compile());
            return this;
        }

        public IPagingRepositoryResult<T> ThenByDescending(Expression<Func<T, object>> keySelector)
        {
            data = data is IOrderedEnumerable<T> orderedData ? orderedData.ThenByDescending(keySelector.Compile()) : data.OrderByDescending(keySelector.Compile());
            return this;
        }

        private IEnumerable<T> getPageDataInternal(int pageNo)
            => data.Skip(pageNo * pageSize).Take(pageSize);
    }
}
