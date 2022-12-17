using TTSS.Infrastructure.Data.Mongo.Models;

namespace TTSS.Infrastructure.Data.Mongo.Tests
{
    public class SimpleMongoDocument : MongoDocumentBase
    {
        public string? Name { get; set; }
    }

    public class StudentMongoDocument : SimpleMongoDocument
    {
        public int SchoolId { get; set; }
    }
}
