using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Concurrent;
using TTSS.Infrastructure.Data.Mongo.Models;

namespace TTSS.Infrastructure.Data.Mongo
{
    public class MongoConnectionStore
    {
        private ConcurrentDictionary<string, MongoClient>? clients;
        private readonly IDictionary<string, MongoConnection> connections = new Dictionary<string, MongoConnection>();

        internal void Add(MongoConnection connection)
            => connections.Add(connection.TypeName, connection);

        internal (string? collectionName, IMongoCollection<T>? collection) GetCollection<T>()
        {
            var typeName = typeof(T).Name;
            if (!connections.TryGetValue(typeName, out var connection))
                throw new ArgumentOutOfRangeException($"Collection '{typeName}' not found.");

            MongoClient? client = default;
            if (!clients?.TryGetValue(connection.ConnectionString, out client) ?? false)
                throw new ArgumentOutOfRangeException($"Database '{connection.DatabaseName}' not found.");

            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(it =>
                {
                    it.AutoMap();
                    it.SetIsRootClass(!connection.IsChild);
                });
            }

            var database = client?.GetDatabase(connection.DatabaseName);
            var collection = database?.GetCollection<T>(connection.CollectionName);
            return (connection.CollectionName, connection.NoDiscriminator ? collection : collection?.OfType<T>());
        }

        internal MongoConnectionStore Build()
        {
            clients ??= new ConcurrentDictionary<string, MongoClient>();
            connections
                .Select(it => it.Value.ConnectionString)
                .Distinct()
                .ToList()
                .ForEach(connString =>
                {
                    var client = new MongoClient(connString);
                    clients.TryAdd(connString, client);
                });
            return this;
        }
    }
}
