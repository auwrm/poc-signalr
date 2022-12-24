namespace TTSS.Infrastructure.Services.Models
{
    public class MessagePack
    {
        public long LastMessageId { get; set; }
        public bool HasMorePages { get; set; }
        public IEnumerable<Message> Messages { get; set; }
    }
}
