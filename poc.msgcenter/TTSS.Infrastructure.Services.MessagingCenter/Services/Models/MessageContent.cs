namespace TTSS.Infrastructure.Services.Models
{
    public abstract class MessageContent
    {
        public abstract string Type { get; }
    }

    public class DynamicContent : MessageContent
    {
        public object Data { get; set; }
        public string ContentType { get; set; }
        public override string Type => MessageType.Dynamic.ToString();
    }

    public class NotificationContent : MessageContent
    {
        public string Message { get; set; }
        public string EndpointUrl { get; set; }
        public override string Type => MessageType.Notification.ToString();
    }
}
