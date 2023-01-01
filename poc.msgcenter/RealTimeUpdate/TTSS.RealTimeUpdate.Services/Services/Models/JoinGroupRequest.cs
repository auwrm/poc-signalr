using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace TTSS.RealTimeUpdate.Services.Models
{
    public class JoinGroupRequest
    {
        public string Secret { get; set; }
        public string GroupName { get; set; }
        public InvocationContext Context { get; set; }
    }
}
