using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using TTSS.Infrastructure.Data.Mongo;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Validators;
using TTSS.RealTimeUpdate.Services.DbModels;
using TTSS.RealTimeUpdate.Services.Models;

namespace TTSS.RealTimeUpdate.Services
{
    public class MessagingCenterHub : IMessagingCenterHub
    {
        private readonly IHubClients hubClients;
        private readonly IGroupManager groupManager;
        private readonly IDateTimeService dateTimeService;
        private readonly IMongoRepository<MessageInfo, string> messageRepo;

        public MessagingCenterHub(IHubClients hubClients,
            IGroupManager groupManager,
            IDateTimeService dateTimeService,
            IMongoRepository<MessageInfo, string> messageRepo)
        {
            this.hubClients = hubClients ?? throw new ArgumentNullException(nameof(hubClients));
            this.groupManager = groupManager ?? throw new ArgumentNullException(nameof(groupManager));
            this.dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
            this.messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
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

        public async Task<SendMessageResponse?> Send(SendMessage message)
        {
            if (!message.Validate())
            {
                return new()
                {
                    ErrorMessage = "Nonce, Filter and TargetGroup can't be null or empty.",
                    NonceStatus = string.IsNullOrEmpty(message?.Nonce)
                        ? new Dictionary<string, bool>()
                        : new() { { message.Nonce, false } },
                };
            }

            var fromTime = dateTimeService.UtcNow.Subtract(TimeSpan.FromMinutes(5));
            var hasDone = messageRepo.Get(it => it.Nonce == message.Nonce && it.CreatedDate >= fromTime).Count() > 0;
            if (hasDone)
            {
                return new()
                {
                    ErrorMessage = "Duplicated request.",
                    NonceStatus = string.IsNullOrEmpty(message?.Nonce)
                        ? new Dictionary<string, bool>()
                        : new() { { message.Nonce, false } },
                };
            }

            var eventId = dateTimeService.UtcNow.Ticks;

            MessageContent content = null;
            if (message is SendMessage<DynamicContent> dynamic)
            {
                content = dynamic.Content;
            }
            else if (message is SendMessage<NotificationContent> noti)
            {
                content = noti.Content;
            }
            else
            {
                throw new NotSupportedException("Not support this message content");
            }

            await messageRepo.InsertAsync(new()
            {
                Id = eventId.ToString(),
                CreatedDate = dateTimeService.UtcNow,
                Filter = message.Filter,
                Nonce = message.Nonce,
                TargetGroups = message.TargetGroups,
                Content = content,
            });

            foreach (var group in message.TargetGroups.Distinct())
            {
                var proxy = hubClients.Group(group);
                if (null == proxy) continue;
                await proxy.SendAsync("update", eventId, message.Filter);
            }

            return new()
            {
                NonceStatus = new Dictionary<string, bool>()
                {
                    { message.Nonce, true }
                }
            };
        }

        public Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.SyncMessage(GetMessages request) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.GetMoreMessages(GetMessages request) => throw new NotImplementedException();
        public Task<bool> UpdateMessageTracker(UpdateMessageTracker request) => throw new NotImplementedException();
        public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}
