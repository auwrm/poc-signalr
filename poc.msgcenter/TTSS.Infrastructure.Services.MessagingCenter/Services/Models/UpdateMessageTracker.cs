namespace TTSS.Infrastructure.Services.Models
{
    public class UpdateMessageTracker
    {
        public string UserId { get; set; }
        public long FromMessageId { get; set; }
        public long ThruMessageId { get; set; }
    }
}
