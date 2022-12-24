namespace TTSS.Infrastructure.Services.Models
{
    public class MessageFilter
    {
        public IEnumerable<string> Scopes { get; set; }
        public IEnumerable<string> Activities { get; set; }
    }
}
