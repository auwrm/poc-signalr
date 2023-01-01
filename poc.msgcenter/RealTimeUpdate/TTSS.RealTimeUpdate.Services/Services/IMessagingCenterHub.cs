using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using TTSS.Infrastructure.Services;
using TTSS.RealTimeUpdate.Services.Models;

namespace TTSS.RealTimeUpdate.Services
{
    public interface IMessagingCenterHub : IMessagingCenter
    {
        Task SendClientSecret(InvocationContext context);
        Task JoinGroup(JoinGroupRequest req);
        Task LeaveGroup(InvocationContext context);
    }
}
