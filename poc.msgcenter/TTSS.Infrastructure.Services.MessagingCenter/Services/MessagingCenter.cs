using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;
using TTSS.Infrastructure.Services.Validators;

namespace TTSS.Infrastructure.Services
{
    public class MessagingCenter : IMessagingCenter
    {
        private readonly IRestService restService;
        private readonly MessagingCenterOptions messagingCenterOptions;

        private string hostFQDN;
        private string HostFQDN => hostFQDN ??= messagingCenterOptions?.HostFQDN;

        public MessagingCenter(IRestService restService,
            MessagingCenterOptions msgCenterOptions)
        {
            this.restService = restService;
            messagingCenterOptions = msgCenterOptions;
        }

        public async Task<JoinGroupResponse> JoinGroup(JoinGroupRequest req)
        {
            if (!req.Validate()) return createError("Parameter invalid.");

            var builder = new UriBuilder(HostFQDN)
            {
                Path = $"/api/{nameof(JoinGroup)}",
            };
            var rsp = await restService.Post<JoinGroupRequest, JoinGroupResponse>(builder.Uri.AbsoluteUri, req);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : createError("Can't send request to the Messaging Center Service");

            JoinGroupResponse createError(string msg)
                => new()
                {
                    Nonce = req?.Nonce,
                    ErrorMessage = msg,
                };
        }

        public Task<SendMessageResponse?> Send(SendMessage message)
            => Send(new[] { message });

        public async Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages)
        {
            if (!messages.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var builder = new UriBuilder(HostFQDN)
            {
                Path = $"/api/{nameof(Send)}",
            };
            var rsp = await restService.Post<IEnumerable<SendMessage>, SendMessageResponse>(builder.Uri.AbsoluteUri, messages);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : createError("Can't send message to the Messaging Center Service", rsp?.Data?.NonceStatus);

            SendMessageResponse createError(string msg, IDictionary<string, bool> status = default)
                => new() { ErrorMessage = msg, NonceStatus = status ?? new Dictionary<string, bool>() };
        }

        public async Task<MessagePack> SyncMessage(GetMessages request)
        {
            if (!request.Validate()) return new() { Messages = Enumerable.Empty<Message>() };

            var scopes = $"scopes={string.Join(',', request.Filter.Scopes.Distinct())}";
            var activities = $"activities={string.Join(',', request.Filter.Activities.Distinct())}";
            var userId = $"userId={request.UserId}";
            var fromGroup = $"fromGroup={request.FromGroup}";
            var fromMessageId = $"fromMessageId={request.FromMessageId}";
            var builder = new UriBuilder(HostFQDN)
            {
                Query = $"{scopes}&{activities}&{userId}&{fromGroup}&{fromMessageId}",
                Path = $"/api/{nameof(SyncMessage)}",
            };
            var rsp = await restService.Get<MessagePack>(builder.Uri.AbsoluteUri);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : new() { Messages = Enumerable.Empty<Message>() };
        }

        public async Task<MessagePack> GetMoreMessages(GetMessages request)
        {
            if (!request.Validate()) return new() { Messages = Enumerable.Empty<Message>() };

            var scopes = $"scopes={string.Join(',', request.Filter.Scopes.Distinct())}";
            var activities = $"activities={string.Join(',', request.Filter.Activities.Distinct())}";
            var userId = $"userId={request.UserId}";
            var fromGroup = $"fromGroup={request.FromGroup}";
            var fromMessageId = $"fromMessageId={request.FromMessageId}";
            var builder = new UriBuilder(HostFQDN)
            {
                Query = $"{scopes}&{activities}&{userId}&{fromGroup}&{fromMessageId}",
                Path = $"/api/{nameof(GetMoreMessages)}",
            };
            var rsp = await restService.Get<MessagePack>(builder.Uri.AbsoluteUri);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : new() { Messages = Enumerable.Empty<Message>() };
        }

        public async Task<bool> UpdateMessageTracker(UpdateMessageTracker request)
        {
            if (!request.Validate()) return false;

            var from = $"from={request.FromMessageId}";
            var thru = $"thru={request.ThruMessageId}";
            var userId = $"userId={request.UserId}";
            var builder = new UriBuilder(HostFQDN)
            {
                Query = $"{from}&{thru}&{userId}",
                Path = $"/api/{nameof(UpdateMessageTracker)}",
            };
            await restService.Put(builder.Uri.AbsoluteUri);
            return true;
        }

        public async Task<bool> ClearAllMessages(ClearAllMessages request)
        {
            if (!request.Validate()) return false;

            var builder = new UriBuilder(HostFQDN);
            await restService.Put(builder.Uri.AbsoluteUri, request);
            return true;
        }
    }
}