using MongoDB.Bson.Serialization.Attributes;
using TTSS.Infrastructure.Models;

namespace TTSS.Infrastructure.Data.Mongo.Models
{
    public abstract class MongoDocumentBase : IDbModel<string>
    {
        [BsonId]
        public string Id { get; set; }
    }
}
