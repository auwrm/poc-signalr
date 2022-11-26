using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace msgcenter.Triggers
{
    public class MessagingTrigger : ServerlessHub
    {
        [FunctionName("negotiate")]
        public Task<SignalRConnectionInfo> NegotiateAsync([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
        {
            var claims = GetClaims(req.Headers["Authorization"]);
            return NegotiateAsync(new NegotiationOptions
            {
                UserId = claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
                Claims = claims
            });
        }

        [FunctionName(nameof(OnConnected))]
        public async Task OnConnected([SignalRTrigger] InvocationContext invocationContext, ILogger logger)
        {
            await Clients.All.SendAsync("newConnection", invocationContext.ConnectionId);
            await Clients.Client(invocationContext.ConnectionId).SendAsync("setConnectionId", invocationContext.ConnectionId);
            logger.LogInformation($"{invocationContext.ConnectionId} has connected");
        }

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext, ILogger logger)
        {
            logger.LogInformation($"{invocationContext.ConnectionId} has disconnected");
        }

        [FunctionName(nameof(SetTopics))]
        public async Task<IActionResult> SetTopics([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest req, ILogger log)
        {
            var qsRefId = req.Query["refid"].ToString();
            var qsJoinTopics = req.Query["join"];
            var qsLeaveTopics = req.Query["leave"];
            log.LogInformation($"[{nameof(SetTopics)}] RefId: {qsRefId}, Topics: {qsJoinTopics}");

            var client = string.IsNullOrWhiteSpace(qsRefId) ? null : Clients.User(qsRefId);
            if (null == client)
            {
                return new BadRequestResult();
            }

            var joinTopics = getTopics(qsJoinTopics);
            foreach (var item in joinTopics)
            {
                await Groups.AddToGroupAsync(qsRefId, item);
                log.LogInformation($"[Join Group] RefId: {qsRefId}, Group: {item}");
            }

            var leaveTopics = getTopics(qsLeaveTopics);
            foreach (var item in leaveTopics)
            {
                await Groups.RemoveFromGroupAsync(qsRefId, item);
                log.LogInformation($"[Leave Group] RefId: {qsRefId}, Group: {item}");
            }
            return new OkResult();
        }

        [FunctionName(nameof(SendMessage))]
        public async Task<IActionResult> SendMessage([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest req, ILogger log)
        {
            var qsRefId = req.Query["refid"];
            var qsTopics = req.Query["topics"];
            var qsMessage = req.Query["msg"];
            log.LogInformation($"[{nameof(SendMessage)}] Msg: {qsMessage}, Topics: {qsTopics}");

            var topics = getTopics(qsTopics);
            var areArgumentsValid = !string.IsNullOrWhiteSpace(qsMessage) && !string.IsNullOrWhiteSpace(qsRefId) && topics.Any();
            if (!areArgumentsValid)
            {
                return new BadRequestResult();
            }

            foreach (var item in topics)
            {
                await Clients.Group(item).SendAsync("newMessage", new NewMessage(qsMessage, item, qsRefId));
            }
            return new OkResult();
        }

        private IEnumerable<string> getTopics(string topics)
            => topics?.Split(",", System.StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();

        private class NewMessage
        {
            public string Text { get; }
            public string Topic { get; set; }
            public string From { get; set; }

            public NewMessage(string message, string topic, string from)
            {
                Text = message;
                Topic = topic;
                From = from;
            }
        }
    }
}