using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using TTSS.Infrastructure.Services;

namespace TTSS.RealTimeUpdate.Services
{
    public interface IMessagingCenterHub : IMessagingCenter
    {
        Task SendClientSecret(InvocationContext context);
        Task LeaveGroup(InvocationContext context);
    }
}
