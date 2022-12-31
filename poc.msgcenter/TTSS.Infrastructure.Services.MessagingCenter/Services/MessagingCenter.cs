using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;
using TTSS.Infrastructure.Services.Validators;

namespace TTSS.Infrastructure.Services
{
    public class MessagingCenter : IMessagingCenter
    {
        private readonly IRestService restService;
        private readonly MessagingCenterOptions messagingCenterOptions;

        private string hostUrl;
        private string HostUrl => hostUrl ??= messagingCenterOptions?.HostUrl;

        public MessagingCenter(IRestService restService,
            MessagingCenterOptions msgCenterOptions)
        {
            this.restService = restService;
            messagingCenterOptions = msgCenterOptions;
        }

        public Task<SendMessageResponse?> Send(SendMessage message)
            => Send(new[] { message });

        public async Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages)
        {
            if (!messages.Validate()) return createError("Nonce, Filter and TargetGroup can't be null or empty.");

            var builder = new UriBuilder
            {
                Host = HostUrl,
                Scheme = "https",
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
            var builder = new UriBuilder
            {
                Host = HostUrl,
                Scheme = "https",
                Query = $"{scopes}&{activities}",
                Path = $"{request.UserId}/{request.FromGroup}/{request.FromMessageId}",
            };
            var rsp = await restService.Get<MessagePack>(builder.Uri.AbsoluteUri);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : new() { Messages = Enumerable.Empty<Message>() };
        }

        public async Task<MessagePack> GetMoreMessages(GetMessages request)
        {
            if (!request.Validate()) return new() { Messages = Enumerable.Empty<Message>() };

            var scopes = $"scopes={string.Join(',', request.Filter.Scopes.Distinct())}";
            var activities = $"activities={string.Join(',', request.Filter.Activities.Distinct())}";
            var builder = new UriBuilder
            {
                Host = HostUrl,
                Scheme = "https",
                Query = $"{scopes}&{activities}",
                Path = $"{request.UserId}/{request.FromGroup}/more/{request.FromMessageId}",
            };
            var rsp = await restService.Get<MessagePack>(builder.Uri.AbsoluteUri);
            return (rsp?.IsSuccessStatusCode ?? false) ? rsp.Data : new() { Messages = Enumerable.Empty<Message>() };
        }

        public async Task<bool> UpdateMessageTracker(UpdateMessageTracker request)
        {
            if (!request.Validate()) return false;

            var from = $"from={request.FromMessageId}";
            var thru = $"thru={request.ThruMessageId}";
            var builder = new UriBuilder
            {
                Host = HostUrl,
                Scheme = "https",
                Query = $"{from}&{thru}",
                Path = request.UserId,
            };
            await restService.Put(builder.Uri.AbsoluteUri);
            return true;
        }

        public async Task<bool> ClearAllMessages(ClearAllMessages request)
        {
            if (!request.Validate()) return false;

            var builder = new UriBuilder
            {
                Host = HostUrl,
                Scheme = "https",
            };
            await restService.Put(builder.Uri.AbsoluteUri, request);
            return true;
        }
    }
}