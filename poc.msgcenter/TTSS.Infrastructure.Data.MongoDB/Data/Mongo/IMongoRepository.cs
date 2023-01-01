using TTSS.Infrastructure.Models;

namespace TTSS.Infrastructure.Data.Mongo
{
    public interface IMongoRepository<T, K> : IOperationalRepository<T, K>,
        IUpsertRepository<T, K>,
        IQueryRepository<T, K>,
        IDeletableRepository<T, K>,
        IInsertBulkRepository<T>
        where T : IDbModel<K>
    {
    }
}
