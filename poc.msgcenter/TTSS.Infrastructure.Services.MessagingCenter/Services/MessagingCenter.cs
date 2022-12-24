﻿using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;

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

        public Task<SendMessageResponse> Send(SendMessage message)
            => Send(new[] { message });

        public async Task<SendMessageResponse> Send(IEnumerable<SendMessage> messages)
        {
            var isArgumentValid = (messages?.Any() ?? false)
                && !messages.Any(it => string.IsNullOrWhiteSpace(it.Nonce) || null == it.Filter || false == (it.TargetGroups?.Any() ?? false));
            if (!isArgumentValid)
            {
                return new SendMessageResponse
                {
                    ErrorMessage = "Nonce, Filter and TargetGroup can't be null or empty.",
                };
            }

            var rsp = await restService.Post<IEnumerable<SendMessage>, SendMessageResponse>(msgCenterHostUrl, messages);
            if (false == (rsp?.IsSuccessStatusCode ?? false))
            {
                return new SendMessageResponse
                {
                    NonceStatus = rsp?.Data.NonceStatus,
                    ErrorMessage = "Can't send message to the Messaging Center Service",
                };
            }

            return rsp?.Data;
        }

        public Task<MessagePack> SyncMessage(GetMessages request) => throw new NotImplementedException();
        public Task<MessagePack> GetNewMessages(GetMessages request) => throw new NotImplementedException();
        public Task<MessagePack> GetMoreMessages(GetMessages request) => throw new NotImplementedException();
        public Task<bool> UpdateMessageTracker(UpdateMessageTracker request) => throw new NotImplementedException();
        public Task<bool> ClearAllMessages(ClearAllMessages request) => throw new NotImplementedException();
    }
}