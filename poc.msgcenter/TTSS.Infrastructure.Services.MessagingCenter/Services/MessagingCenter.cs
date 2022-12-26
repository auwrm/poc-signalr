using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;
using TTSS.Infrastructure.Services.Validators;

namespace TTSS.Infrastructure.Services
{
    public class MessagingCenter : IMessagingCenter
    {
        private readonly IRestService restService;
        private readonly string msgCenterHostUrl;

        public MessagingCenter(IRestService restService,
            MessagingCenterOptions msgCenterOptions)
        {
            this.restService = restService;
            msgCenterHostUrl = msgCenterOptions?.HostUrl ?? throw new ArgumentNullException(nameof(msgCenterOptions));
        }

        public Task<SendMessageResponse?> Send(SendMessage message)
            => Send(new[] { message });

        public async Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages)
        {
            if (!messages.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var rsp = await restService.Post<IEnumerable<SendMessage>, SendMessageResponse>(msgCenterHostUrl, messages);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : createError("Can't send message to the Messaging Center Service", rsp?.Data?.NonceStatus);

            SendMessageResponse createError(string msg, IDictionary<string, bool> status = default)
                => new() { ErrorMessage = msg, NonceStatus = status ?? new Dictionary<string, bool>() };
        }

        public async Task<MessagePack> SyncMessage(GetMessages request)
        {
            if (!request.Validate()) return new() { Messages = Enumerable.Empty<Message>() };

            var builder = new UriBuilder(msgCenterHostUrl);
            builder.Scheme = "https";
            builder.Path = $"{request.UserId}/{request.FromGroup}/{request.FromMessageId}";
            var scopes = $"scopes={string.Join(',', request.Filter.Scopes.Distinct())}";
            var activities = $"activities={string.Join(',', request.Filter.Activities.Distinct())}";
            builder.Query = $"{scopes}&{activities}";
            var rsp = await restService.Get<MessagePack>(builder.Uri.AbsoluteUri);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : new() { Messages = Enumerable.Empty<Message>() };
        }

        public Task<MessagePack> GetNewMessages(GetMessages request) => throw new NotImplementedException();
        public Task<MessagePack> GetMoreMessages(GetMessages request) => throw new NotImplementedException();
        public Task<bool> UpdateMessageTracker(UpdateMessageTracker request) => throw new NotImplementedException();
        public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}