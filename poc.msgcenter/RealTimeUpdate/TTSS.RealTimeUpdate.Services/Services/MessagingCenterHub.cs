using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using MongoDB.Driver.Linq;
using System.Diagnostics;
using System.Linq;
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
        private readonly IMongoRepository<MessageTrack, string> messageTrackRepo;

        public MessagingCenterHub(IDateTimeService dateTimeService,
            IMongoRepository<MessageInfo, string> messageRepo,
            IServiceHubContext hubContext,
            IServiceManager serviceManager,
            IMongoRepository<MessageTrack, string> messageTrackRepo)
            : base(hubContext, serviceManager)
        {
            this.dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
            this.messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo));
            this.messageTrackRepo = messageTrackRepo ?? throw new ArgumentNullException(nameof(messageTrackRepo));
        }

        public async Task<JoinGroupResponse> JoinGroup(JoinGroupRequest req)
        {
            Console.WriteLine("------------- JoinGroup");
            if (!req.Validate()) return createError();

            var client = Clients.User(req.Secret);
            if (null == client) return createError();

            Console.WriteLine($"---------- JoinGroup Secret= ${req.Secret} GroupName= {req.GroupName}");
            await Groups.AddToGroupAsync(req.Secret, req.GroupName);
            return new()
            {
                Nonce = req.Nonce,
                JoinGroupName = req.GroupName,
            };

            JoinGroupResponse createError(string msg = "Invalid message, some parameters are invalid or missing")
                => new()
                {
                    Nonce = req?.Nonce,
                    ErrorMessage = msg,
                };
        }

        public async Task SendClientSecret(InvocationContext context)
        {
            Console.WriteLine("+++++++++++++++ SendClientSecret");
            var isArgumentValid = !string.IsNullOrWhiteSpace(context?.ConnectionId);
            if (!isArgumentValid) return;

            var client = Clients?.Client(context.ConnectionId);
            if (null == client) return;

            Console.WriteLine($"+++++++++++++++ SendClientSecret ConnectionId= {context.ConnectionId}");
            await client.SendAsync("setClientId", context.ConnectionId);
        }

        public Task LeaveGroup(InvocationContext context) => throw new NotImplementedException();

        public async Task<SendMessageResponse?> Send(SendMessage message)
        {
            Console.WriteLine("************* Send 11111 ");
            if (!message.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var fromTime = dateTimeService.UtcNow.Subtract(TimeSpan.FromMinutes(5));
            var hasDone = messageRepo.Get(it => it.Nonce == message.Nonce && it.CreatedDate >= fromTime).Count() > 0;
            if (hasDone) return createError("Duplicated message.");

            var content = message switch
            {
                SendMessage<MessageContent> msg => msg.Content,
                SendMessage<DynamicContent> msg => msg.Content,
                SendMessage<NotificationContent> msg => msg.Content,
                _ => null,
            };

            Console.WriteLine($"************* Send 11111 ************* {content}");
            if (null == content) throw new NotSupportedException("Not support this message content");

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

            // TODO: Send to groups must be done in the Send(IEnumerable<SendMessage>) for improve performance.
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
            Console.WriteLine("************* Send 222222 ");
            if (!messages.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var errorMsg = string.Empty;
            var dic = Enumerable.Empty<KeyValuePair<string, bool>>();
            foreach (var message in messages)
            {
                Console.WriteLine($"************* Send 222222 *************{message.Nonce}");
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

        public Task<Infrastructure.Services.Models.MessagePack> SyncMessage(GetMessages messages)
        {
            Console.WriteLine("------------- SyncMessage");

            if (!messages.Validate())
                return Task.FromResult(new Infrastructure.Services.Models.MessagePack()
                {
                    Messages = Enumerable.Empty<Message>()
                });

            // TODO: What UserId of filter ?
            var msgInfos = messageRepo.Get(it => it.TargetGroups.Contains(messages.FromGroup));

            var resultMsgs = new List<Message<MessageContent>>();
            foreach (var m in msgInfos)
            {
                if (m is null) { continue; }
                var mContent = new Message<MessageContent>(m.Content)
                {
                    Id = long.Parse(m.Id),
                    HasSeen = false,
                    CreatedDate = m.CreatedDate
                };
                resultMsgs.Add(mContent);
            }

            var messagePack = new Infrastructure.Services.Models.MessagePack()
            {
                HasMorePages = false,
                LastMessageId = messages.FromMessageId,
                Messages = resultMsgs,
            };

            return Task.FromResult(messagePack);
        }

        public Task<Infrastructure.Services.Models.MessagePack> GetMoreMessages(GetMessages messages)
        {
            Console.WriteLine("------------- GetMoreMessages");

            if (!messages.Validate())
                return Task.FromResult(new Infrastructure.Services.Models.MessagePack()
                {
                    Messages = Enumerable.Empty<Message>()
                });

            var msgInfos = messageRepo.Get(); // TODO: What filter ?

            var resultMsgs = new List<Message<MessageContent>>();
            foreach (var m in msgInfos)
            {
                if (m is null) { continue; }
                var mContent = new Message<MessageContent>(m.Content)
                {
                    Id = long.Parse(m.Id),
                    HasSeen = false, // TODO: don't no, Seen or not seen
                    CreatedDate = m.CreatedDate
                };
                resultMsgs.Add(mContent);
            }

            var messagePack = new Infrastructure.Services.Models.MessagePack()
            {
                HasMorePages = false,
                LastMessageId = messages.FromMessageId,
                Messages = resultMsgs,
            };

            return Task.FromResult(messagePack);
        }

        public Task<bool> UpdateMessageTracker(UpdateMessageTracker message)
        {
            if (!message.Validate()) return Task.FromResult(message.Validate());

            var msgInfo = messageTrackRepo.Get().First(); // TODO: What filter ?

            //messageRepo.UpdateAsync(msgInfo.Id, msgInfo);
            throw new NotImplementedException();
        }

        public Task<bool> ClearAllMessages(ClearAllMessages request)
        {
            throw new NotImplementedException();
        }

        //Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.SyncMessage(GetMessages message) => throw new NotImplementedException();
        //Task<Infrastructure.Services.Models.MessagePack> IMessagingCenter.GetMoreMessages(GetMessages message) => throw new NotImplementedException();
        //public Task<bool> UpdateMessageTracker(UpdateMessageTracker message) => throw new NotImplementedException();
        //public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}
