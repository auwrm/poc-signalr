using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TTSS.Infrastructure.Data.Mongo;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.RealTimeUpdate.Services;
using TTSS.RealTimeUpdate.Services.DbModels;

namespace TTSS.RealTimeUpdate.Triggers
{
    public class MessagingTrigger : MessagingCenterHub
    {
        public MessagingTrigger(IDateTimeService dateTimeService,
            IMongoRepository<MessageInfo, string> messageRepo,
            IServiceHubContext hubContext = null, IServiceManager serviceManager = null)
            : base(dateTimeService, messageRepo, hubContext, serviceManager)
        {
        }

        [FunctionName(nameof(Negotiate))]
        public Task<SignalRConnectionInfo> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = nameof(MessagingTrigger))] SignalRConnectionInfo connectionInfo)
            => Task.FromResult(connectionInfo);

        [FunctionName(nameof(OnConnected))]
        public Task OnConnected([SignalRTrigger] InvocationContext invocationContext)
            => SendClientSecret(invocationContext);

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext)
            => LeaveGroup(invocationContext);

        [FunctionName(nameof(JoinGroup))]
        public Task<JoinGroupResponse> JoinGroupAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] JoinGroupRequest body)
            => JoinGroup(body);
    }
}
