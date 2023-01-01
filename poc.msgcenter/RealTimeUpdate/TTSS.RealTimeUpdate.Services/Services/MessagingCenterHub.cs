using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.RealTimeUpdate.Services.Models;

namespace TTSS.RealTimeUpdate.Services
{
    public class MessagingCenterHub : IMessagingCenterHub
    {
        private readonly IHubClients clients;

        public MessagingCenterHub(IHubClients hubClients)
            => clients = hubClients;

        public async Task SendClientSecret(InvocationContext context)
        {
            var isArgumentValid = !string.IsNullOrWhiteSpace(context?.ConnectionId);
            if (!isArgumentValid) return;

            var client = clients?.Client(context.ConnectionId);
            if (null == client) return;

            await client.SendAsync("setClientId", context.ConnectionId);
        }
        public Task JoinGroup(JoinGroupRequest req) => throw new NotImplementedException();
        public Task LeaveGroup(InvocationContext context) => throw new NotImplementedException();

        public Task<SendMessageResponse?> Send(SendMessage message) => throw new NotImplementedException();
        public Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.SyncMessage(GetMessages request) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.GetMoreMessages(GetMessages request) => throw new NotImplementedException();
        public Task<bool> UpdateMessageTracker(UpdateMessageTracker request) => throw new NotImplementedException();
        public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}
