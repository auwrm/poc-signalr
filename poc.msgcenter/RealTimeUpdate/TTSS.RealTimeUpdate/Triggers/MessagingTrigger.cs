using Microsoft.AspNetCore.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
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

        [FunctionName(nameof(Send))]
        public async Task<SendMessageResponse> SendAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage body)
        {
            var requestBodyText = await body.Content.ReadAsStringAsync();
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new SendMessageConverter());
            var messages = JsonSerializer.Deserialize<IEnumerable<SendMessage<MessageContent>>>(requestBodyText, deserializeOptions)!;
            return await Send(messages);
        }
    }
}
