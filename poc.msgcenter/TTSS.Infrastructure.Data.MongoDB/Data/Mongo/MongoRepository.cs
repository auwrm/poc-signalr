using MongoDB.Driver;
using System.Linq.Expressions;
using TTSS.Infrastructure.Models;

namespace TTSS.Infrastructure.Data.Mongo
{
    public class MongoRepository<T, K> : IMongoRepository<T, K>
        where T : IDbModel<K>
    {
        protected internal readonly string CollectionName;
        protected internal readonly IMongoCollection<T> Collection;
        private readonly Expression<Func<T, K>> idField;

        public int BatchSize { get; set; } = 100;
        public bool BypassDocumentValidation { get; set; } = true;

        public MongoRepository(MongoConnectionStore connectionStore, Expression<Func<T, K>> idField)
        {
            var collection = connectionStore.GetCollection<T>();
            Collection = collection.collection ?? throw new ArgumentOutOfRangeException(nameof(collection.collection));
            CollectionName = collection.collectionName ?? throw new ArgumentOutOfRangeException(nameof(collection.collectionName));
            this.idField = idField ?? throw new ArgumentNullException(nameof(idField));
        }

        public Task<T> GetByIdAsync(K key, CancellationToken cancellationToken = default)
        {
            var idKey = GetEntityFilter(key);
            return Collection.Find(idKey).FirstOrDefaultAsync(cancellationToken);
        }

        public IEnumerable<T> Get(CancellationToken cancellationToken = default)
            => new MongoQueryResult<T>(Collection.Find(Builders<T>.Filter.Empty), cancellationToken);

        public IEnumerable<T> Get(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
            => new MongoQueryResult<T>(Collection.Find(filter), cancellationToken);

        public Task InsertAsync(T data, CancellationToken cancellationToken = default)
            => Collection.InsertOneAsync(data, new InsertOneOptions
            {
                BypassDocumentValidation = BypassDocumentValidation
            }, cancellationToken);

        public async Task<bool> UpdateAsync(K key, T data, CancellationToken cancellationToken = default)
        {
            var idKey = GetEntityFilter(key);
            var result = await Collection.ReplaceOneAsync(idKey, data, new ReplaceOptions
            {
                IsUpsert = false,
                BypassDocumentValidation = BypassDocumentValidation,
            }, cancellationToken);
            return result.IsAcknowledged && result.MatchedCount > 0;
        }

        public async Task<bool> UpsertAsync(K key, T data, CancellationToken cancellationToken = default)
        {
            var idKey = GetEntityFilter(key);
            var result = await Collection.ReplaceOneAsync(idKey, data, new ReplaceOptions
            {
                IsUpsert = true,
                BypassDocumentValidation = BypassDocumentValidation,
            }, cancellationToken);
            return result.IsAcknowledged && result.MatchedCount > 0;
        }

        public async Task<bool> DeleteAsync(K key, CancellationToken cancellationToken = default)
        {
            var idKey = GetEntityFilter(key);
            var result = await Collection.DeleteOneAsync(idKey, cancellationToken);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        public async Task<bool> DeleteManyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
        {
            var result = await Collection.DeleteManyAsync<T>(filter, cancellationToken);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        public async Task InsertBulkAsync(IEnumerable<T> data, CancellationToken cancellationToken = default)
        {
            var batch = data.Take(BatchSize).ToList();
            data = data.Skip(BatchSize).ToList();

            while (batch.Any())
            {
                var startRequestDateTime = DateTime.Now;

                try
                {
                    await Collection.InsertManyAsync(batch,
                        new InsertManyOptions { BypassDocumentValidation = BypassDocumentValidation },
                        cancellationToken);
                }
                catch (MongoBulkWriteException<T> ex)
                {
                    // in case of request rate limit
                    if (ex.WriteErrors.Count > 0 && ex.WriteErrors.Any(it => it.Code == 16500))
                    {
                        batch = batch.Skip((int)ex.Result.InsertedCount).ToList();
                        var now = DateTime.Now;
                        var usedTime = now - startRequestDateTime;
                        var mustWait = Math.Min(9, 1009 - usedTime.Milliseconds);
                        var backOff = Convert.ToInt32(now.Ticks % 30);
                        await Task.Delay(mustWait + backOff);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
                batch = data.Take(BatchSize).ToList();
                data = data.Skip(BatchSize).ToList();
            }
        }

        protected virtual FilterDefinition<T> GetEntityFilter(K key)
            => Builders<T>.Filter.Eq(idField, key);
    }
}
