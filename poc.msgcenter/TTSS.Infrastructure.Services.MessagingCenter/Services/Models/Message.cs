namespace TTSS.Infrastructure.Services.Models
{
    public abstract class Message
    {
        public long Id { get; set; }
        public bool HasSeen { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Message<T> : Message
        where T : MessageContent
    {
        public T Content { get; set; }

        public Message(T content)
            => Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}
