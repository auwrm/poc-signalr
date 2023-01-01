using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.RealTimeUpdate.Services.Models;

namespace TTSS.RealTimeUpdate.Services
{
    public class MessagingCenterHub : IMessagingCenterHub
    {
        private readonly IHubClients hubClients;
        private readonly IGroupManager groupManager;

        public MessagingCenterHub(IHubClients hubClients,
            IGroupManager groupManager)
        {
            this.hubClients = hubClients ?? throw new ArgumentNullException(nameof(hubClients));
            this.groupManager = groupManager ?? throw new ArgumentNullException(nameof(groupManager));
        }

        public async Task SendClientSecret(InvocationContext context)
        {
            var isArgumentValid = !string.IsNullOrWhiteSpace(context?.ConnectionId);
            if (!isArgumentValid) return;

            var client = hubClients?.Client(context.ConnectionId);
            if (null == client) return;

            await client.SendAsync("setClientId", context.ConnectionId);
        }

        public async Task<bool> JoinGroup(JoinGroupRequest req)
        {
            var isArgumentValid = !string.IsNullOrWhiteSpace(req?.Secret)
                && !string.IsNullOrWhiteSpace(req?.GroupName);
            if (!isArgumentValid) return false;

            var client = hubClients.User(req.Secret);
            if (null == client) return false;

            await groupManager.AddToGroupAsync(req.Secret, req.GroupName);
            return true;
        }

        public Task LeaveGroup(InvocationContext context) => throw new NotImplementedException();

        public Task<SendMessageResponse?> Send(SendMessage message) => throw new NotImplementedException();
        public Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.SyncMessage(GetMessages request) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.GetMoreMessages(GetMessages request) => throw new NotImplementedException();
        public Task<bool> UpdateMessageTracker(UpdateMessageTracker request) => throw new NotImplementedException();
        public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}
