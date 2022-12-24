namespace TTSS.Infrastructure.Services.Models
{
    public abstract class MessageContent
    {
        public abstract string Type { get; }
    }

    public class DynamicContent<T> : MessageContent
        where T : class, new()
    {
        public T Content { get; set; }
        public string ContentType => typeof(T).Name;
        public override string Type => MessageType.Dynamic.ToString();
    }

    public class NotificationContent : MessageContent
    {
        public string Message { get; set; }
        public string EndpointUrl { get; set; }
        public override string Type => MessageType.Notification.ToString();
    }
}
