using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.Security.Claims;
using TTSS.RealTimeUpdate.Services;

namespace TTSS.RealTimeUpdate.Triggers
{
    public class MessagingTrigger : ServerlessHub
    {
        private readonly IMessagingCenterHub hub;

        public MessagingTrigger(IMessagingCenterHub hub)
            => this.hub = hub;

        [FunctionName("negotiate")]
        public Task<SignalRConnectionInfo> NegotiateAsync([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
        {
            var claims = GetClaims(req.Headers["Authorization"]);
            return NegotiateAsync(new NegotiationOptions
            {
                UserId = claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
                Claims = claims,
            });
        }

        [FunctionName(nameof(OnConnected))]
        public Task OnConnected([SignalRTrigger] InvocationContext invocationContext)
            => hub.SendClientSecret(invocationContext);

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext)
            => hub.LeaveGroup(invocationContext);
    }
}
