using TTSS.Infrastructure.Data.Mongo.Models;
using TTSS.Infrastructure.Services.Models;

namespace TTSS.RealTimeUpdate.Services.DbModels
{
    public class MessageInfo : MongoDocumentBase
    {
        public string Type => Content.Type;
        public MessageFilter Filter { get; set; }
        public IEnumerable<string> TargetGroups { get; set; }
        public MessageContent Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Nonce { get; set; }
    }
}
