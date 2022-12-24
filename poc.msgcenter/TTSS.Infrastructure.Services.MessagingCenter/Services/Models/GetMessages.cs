namespace TTSS.Infrastructure.Services.Models
{
    public class GetMessages
    {
        public long FromMessageId { get; set; }
        public MessageFilter Filter { get; set; }
        public string UserId { get; set; }
        public string FromGroup { get; set; }
    }
}
