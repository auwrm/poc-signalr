using Microsoft.AspNetCore.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System;
using System.Collections.Generic;
using System.Net.Http;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using TTSS.Infrastructure.Data.Mongo;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.RealTimeUpdate.Services;
using TTSS.RealTimeUpdate.Services.DbModels;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TTSS.RealTimeUpdate.Triggers
{
    public class MessagingTrigger : MessagingCenterHub
    {
        private string formatDate = "MM/dd/yyy HH:mm:ss.fff";

        public MessagingTrigger(IDateTimeService dateTimeService,
            IMongoRepository<MessageInfo, string> messageRepo,
            IServiceHubContext hubContext = null,
            IServiceManager serviceManager = null,
            IMongoRepository<MessageTrack, string> messageTrack = null)
            : base(dateTimeService, messageRepo, hubContext, serviceManager, messageTrack)
        {
        }

        [FunctionName(nameof(Negotiate))]
        public Task<SignalRConnectionInfo> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = nameof(MessagingTrigger))] SignalRConnectionInfo connectionInfo)

        {
            Console.WriteLine($"============= Negotiate ============= {DateTime.Now.ToString(formatDate)}");
            return Task.FromResult(connectionInfo);
        }

        [FunctionName(nameof(OnConnected))]
        public Task OnConnected([SignalRTrigger] InvocationContext invocationContext)
        {
            Console.WriteLine($"+++++++++++++++ OnConnected +++++++++++++++ {DateTime.Now.ToString(formatDate)}");
            return SendClientSecret(invocationContext);
        }

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext)
        {
            Console.WriteLine($"-+-+-+-+-+-+-+ OnDisconnected -+-+-+-+-+-+-+ {DateTime.Now.ToString(formatDate)}");
            Console.WriteLine("-+-+-+-+-+-+-+ LeaveGroup not implement");
            //LeaveGroup(invocationContext);
        }

        [FunctionName(nameof(JoinGroup))]
        public Task<JoinGroupResponse> JoinGroupAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] JoinGroupRequest body)
        {
            Console.WriteLine($"------------- JoinGroup ------------- {DateTime.Now.ToString(formatDate)}");
            return JoinGroup(body);
        }

        [FunctionName(nameof(Send))]
        public async Task<SendMessageResponse> SendAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage body)
        {
            Console.WriteLine($"************* SendAsync ************* {DateTime.Now.ToString(formatDate)}");
            var requestBodyText = await body.Content.ReadAsStringAsync();
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new SendMessageConverter());
            var messages = JsonSerializer.Deserialize<IEnumerable<SendMessage<MessageContent>>>(requestBodyText, deserializeOptions)!;
            return await Send(messages);
        }

        [FunctionName(nameof(SyncMessage))]
        public async Task<Infrastructure.Services.Models.MessagePack> SyncMessageAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            Console.WriteLine($"=====----- SyncMessageAsync =====----- {DateTime.Now.ToString(formatDate)}");

            var messages = new GetMessages
            {
                UserId = req.Query["userId"],
                FromGroup = req.Query["fromGroup"],
                FromMessageId = long.Parse(req.Query["fromMessageId"]),
                Filter = new MessageFilter
                {
                    Activities = req.Query["activities"],
                    Scopes = req.Query["scopes"],
                }
            };
            return await SyncMessage(messages);
        }

        [FunctionName(nameof(GetMoreMessages))]
        public Task<Infrastructure.Services.Models.MessagePack> GetMoreMessagesAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            Console.WriteLine($"=====----- SyncMessageAsync =====----- {DateTime.Now.ToString(formatDate)}");

            var messages = new GetMessages
            {
                UserId = req.Query["userId"],
                FromGroup = req.Query["fromGroup"],
                FromMessageId = long.Parse(req.Query["fromMessageId"]),
                Filter = new MessageFilter
                {
                    Activities = req.Query["activities"],
                    Scopes = req.Query["scopes"],
                }
            };
            return GetMoreMessages(messages);
        }

        [FunctionName(nameof(UpdateMessageTracker))]
        public Task<bool> UpdateMessageTrackerAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "UpdateMessageTracker/{userId:alpha}")] HttpRequest req, string userId)
        //[HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequest req)
        {
            Console.WriteLine($"=====----- UpdateMessageTracker =====----- {DateTime.Now.ToString(formatDate)}");

            var messages = new UpdateMessageTracker
            {
                UserId = userId,
                FromMessageId = long.Parse(req.Query["fromMessageId"]),
                ThruMessageId = long.Parse(req.Query["thruMessageId"]),
            };
            return UpdateMessageTracker(messages);
        }
    }
}
