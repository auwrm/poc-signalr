using System.Linq.Expressions;

namespace TTSS.Infrastructure.Data
{
    public class InMemoryRepository<T, K> : IOperationalRepository<T, K>,
        IUpsertRepository<T, K>,
        IQueryRepository<T, K>,
        IQueryableRepository<T>,
        IDeletableRepository<T, K>
    {
        private readonly IDictionary<K, T> dataDict;

        protected Func<T, K> GetKey { get; }

        public InMemoryRepository(Expression<Func<T, K>> idField)
        {
            if (idField is null) throw new ArgumentNullException(nameof(idField));
            dataDict = new Dictionary<K, T>();
            GetKey = idField.Compile();
        }

        public Task<T> GetByIdAsync(K key, CancellationToken cancellationToken = default)
            => Task.FromResult(dataDict.TryGetValue(key, out var data) ? data : default);

        public IEnumerable<T> Get(CancellationToken cancellationToken = default)
            => new InMemoryQueryResult<T>(dataDict.Values);

        public IEnumerable<T> Get(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
            => new InMemoryQueryResult<T>(dataDict.Values.Where(filter.Compile()));

        public IQueryable<T> Query(CancellationToken cancellationToken = default)
            => dataDict.Values.AsQueryable();

        public Task InsertAsync(T data, CancellationToken cancellationToken = default)
            => UpsertAsync(GetKey(data), data, cancellationToken);

        public Task<bool> UpsertAsync(K key, T data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            dataDict[key] = data;
            return Task.FromResult(true);
        }

        public Task<bool> UpdateAsync(K key, T data, CancellationToken cancellationToken = default)
            => dataDict.ContainsKey(key) ? UpsertAsync(GetKey(data), data, cancellationToken) : Task.FromResult(false);

        public Task<bool> DeleteAsync(K key, CancellationToken cancellationToken = default)
        {
            if (!dataDict.TryGetValue(key, out var data)) return Task.FromResult(false);
            dataDict.Remove(key);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteManyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
        {
            var predicate = filter.Compile();
            var deleteIds = dataDict.Where(keyValue => predicate(keyValue.Value))
                .Select(it => it.Key)
                .ToList();
            if (!deleteIds.Any()) return Task.FromResult(false);
            deleteIds.ForEach(key => dataDict.Remove(key));
            return Task.FromResult(true);
        }
    }
}
