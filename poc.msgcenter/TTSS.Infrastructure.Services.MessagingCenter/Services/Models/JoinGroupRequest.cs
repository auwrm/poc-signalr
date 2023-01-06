namespace TTSS.Infrastructure.Services.Models
{
    public class JoinGroupRequest
    {
        public string Nonce { get; set; }
        public string Secret { get; set; }
        public string GroupName { get; set; }
    }
}
