namespace TTSS.Infrastructure.Services.Models
{
    public class SendMessageResponse
    {
        public string ErrorMessage { get; set; }
        public IDictionary<string, bool> NonceStatus { get; set; }
    }
}
