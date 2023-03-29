using TTSS.Infrastructure.Data.Mongo.Models;

namespace TTSS.RealTimeUpdate.Services.DbModels
{
    public class MessageTrack : MongoDocumentBase
    {
        public string UserId { get; set; }
        public long FromEventId { get; set; }
        public long ThruEventId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
