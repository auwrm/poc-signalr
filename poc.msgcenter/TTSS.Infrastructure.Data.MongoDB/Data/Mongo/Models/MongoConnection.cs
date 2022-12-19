using TTSS.Infrastructure.Models;

namespace TTSS.Infrastructure.Data.Mongo.Models
{
    public class MongoConnection
    {
        public string TypeName { get; init; }
        public string CollectionName { get; init; }
        public string DatabaseName { get; init; }
        public string ConnectionString { get; init; }
        public bool NoDiscriminator { get; set; }
        public bool IsChild { get; set; }
    }
    public class MongoConnection<T> : MongoConnection
        where T : IDbModelBase
    {
        public MongoConnection(string collectionName, string databaseName, string connectionString)
        {
            TypeName = typeof(T).Name;
            CollectionName = collectionName;
            DatabaseName = databaseName;
            ConnectionString = connectionString;
        }

        public MongoConnection(string databaseName, string connectionString)
            : this(typeof(T).Name, databaseName, connectionString)
        {
        }
    }
}
