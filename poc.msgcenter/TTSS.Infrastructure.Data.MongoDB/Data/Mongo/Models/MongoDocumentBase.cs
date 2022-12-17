using MongoDB.Bson.Serialization.Attributes;

namespace TTSS.Infrastructure.Data.Mongo.Models
{
    public abstract class MongoDocumentBase
    {
        [BsonId]
        public string Id { get; set; }
    }
}
