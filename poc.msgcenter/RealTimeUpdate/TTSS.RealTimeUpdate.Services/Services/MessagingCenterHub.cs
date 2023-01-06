using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using TTSS.Infrastructure.Data.Mongo;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Validators;
using TTSS.RealTimeUpdate.Services.DbModels;

namespace TTSS.RealTimeUpdate.Services
{
    public class MessagingCenterHub : ServerlessHub, IMessagingCenterHub
    {
        private readonly IDateTimeService dateTimeService;
        private readonly IMongoRepository<MessageInfo, string> messageRepo;

        public MessagingCenterHub(IDateTimeService dateTimeService,
            IMongoRepository<MessageInfo, string> messageRepo,
            IServiceHubContext hubContext,
            IServiceManager serviceManager)
            : base(hubContext, serviceManager)
        {
            this.dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
            this.messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
        }

        public async Task<JoinGroupResponse> JoinGroup(JoinGroupRequest req)
        {
            if (!req.Validate()) return createError();

            var client = Clients.User(req.Secret);
            if (null == client) return createError();

            await Groups.AddToGroupAsync(req.Secret, req.GroupName);
            return new()
            {
                Nonce = req.Nonce,
                JoinGroupName = req.GroupName,
            };

            JoinGroupResponse createError(string msg = "Invalid request, some parameters are invalid or missing")
                => new()
                {
                    Nonce = req?.Nonce,
                    ErrorMessage = msg,
                };
        }

        public async Task SendClientSecret(InvocationContext context)
        {
            var isArgumentValid = !string.IsNullOrWhiteSpace(context?.ConnectionId);
            if (!isArgumentValid) return;

            var client = Clients?.Client(context.ConnectionId);
            if (null == client) return;

            await client.SendAsync("setClientId", context.ConnectionId);
        }

        public Task LeaveGroup(InvocationContext context) => throw new NotImplementedException();

        public async Task<SendMessageResponse?> Send(SendMessage message)
        {
            if (!message.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var fromTime = dateTimeService.UtcNow.Subtract(TimeSpan.FromMinutes(5));
            var hasDone = messageRepo.Get(it => it.Nonce == message.Nonce && it.CreatedDate >= fromTime).Count() > 0;
            if (hasDone) return createError("Duplicated request.");

            // TODO: Simplify the DB model
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

            // TODO: Simplify the DB model
            var eventId = dateTimeService.UtcNow.Ticks;
            await messageRepo.InsertAsync(new()
            {
                Id = eventId.ToString(),
                Content = content,
                Nonce = message.Nonce,
                CreatedDate = dateTimeService.UtcNow,
                Filter = new()
                {
                    Scopes = message.Filter.Scopes.Distinct(),
                    Activities = message.Filter.Activities.Distinct(),
                },
                TargetGroups = message.TargetGroups.Distinct(),
            });

            foreach (var group in message.TargetGroups.Distinct())
            {
                var proxy = Clients.Group(group);
                if (null == proxy) continue;
                await proxy.SendAsync("update", eventId, message.Filter);
            }

            return new()
            {
                NonceStatus = new Dictionary<string, bool>
                {
                    { message.Nonce, true }
                }
            };

            SendMessageResponse createError(string error)
                => new()
                {
                    ErrorMessage = error,
                    NonceStatus = string.IsNullOrEmpty(message?.Nonce)
                        ? new Dictionary<string, bool>()
                        : new() { { message.Nonce, false } },
                };
        }

        public async Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages)
        {
            if (!messages.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var errorMsg = string.Empty;
            var dic = Enumerable.Empty<KeyValuePair<string, bool>>();
            foreach (var message in messages)
            {
                var rsp = await Send(message);
                if (null == rsp)
                {
                    dic = dic.Union(new[] { new KeyValuePair<string, bool>(message.Nonce, false) });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(rsp.ErrorMessage))
                    {
                        errorMsg = rsp.ErrorMessage;
                    }
                    dic = dic.Union(rsp.NonceStatus);
                }
            }

            return new()
            {
                ErrorMessage = errorMsg,
                NonceStatus = dic.ToDictionary(it => it.Key, it => it.Value),
            };

            SendMessageResponse createError(string error)
                => new()
                {
                    ErrorMessage = error,
                    NonceStatus = messages?
                        .Where(it => !string.IsNullOrWhiteSpace(it?.Nonce))
                        .Select(it => it.Nonce)
                        .ToDictionary(it => it, _ => false)
                        ?? new(),
                };
        }

        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.SyncMessage(GetMessages request) => throw new NotImplementedException();
        Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.GetMoreMessages(GetMessages request) => throw new NotImplementedException();
        public Task<bool> UpdateMessageTracker(UpdateMessageTracker request) => throw new NotImplementedException();
        public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}
