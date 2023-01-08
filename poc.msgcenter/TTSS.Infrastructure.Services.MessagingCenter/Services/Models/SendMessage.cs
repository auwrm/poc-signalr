namespace TTSS.Infrastructure.Services.Models
{
    public abstract class SendMessage
    {
        public string Nonce { get; set; }
        public MessageFilter Filter { get; set; }
        public IEnumerable<string> TargetGroups { get; set; }
    }

    public class SendMessage<T> : SendMessage
        where T : MessageContent
    {
        public T Content { get; set; }
    }
}
