using TTSS.Infrastructure.Data.Mongo.Models;

namespace TTSS.Infrastructure.Data.Mongo
{
    public class MongoConnectionStoreBuilder
    {
        private MongoConnectionStore mongoConnectionStore = new MongoConnectionStore();

        private string currentDatabaseName;
        private string currentConnectionString;

        public MongoConnectionStoreBuilder(string databaseName, string connectionString)
            => SetupDatabase(databaseName, connectionString);

        public MongoConnectionStoreBuilder SetupDatabase(string databaseName, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException(nameof(databaseName));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException(nameof(connectionString));

            currentDatabaseName = databaseName;
            currentConnectionString = connectionString;
            return this;
        }

        public MongoConnectionStoreBuilder RegisterCollection<T>(string? collectionName = default, bool noDiscriminator = default, bool isChild = false)
            where T : class, new()
        {
            var connection = string.IsNullOrEmpty(collectionName)
                ? new MongoConnection<T>(currentDatabaseName, currentConnectionString)
                : new MongoConnection<T>(collectionName, currentDatabaseName, currentConnectionString);
            connection.IsChild = isChild;
            connection.NoDiscriminator = noDiscriminator;
            mongoConnectionStore.Add(connection);
            return this;
        }

        public MongoConnectionStore Build()
        {
            var store = mongoConnectionStore.Build();
            mongoConnectionStore = new MongoConnectionStore();
            return store;
        }
    }
}
