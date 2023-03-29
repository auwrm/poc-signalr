using Flurl.Util;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;

namespace TTSS.RealTimeUpdate.Client
{
    internal class DemoClientSentMessage
    {
        private readonly HubConnection hubConnection;
        private readonly IMessagingCenter messagingCenter;
        private readonly string groupName;
        private readonly string formatDate;

        public DemoClientSentMessage(HubConnection hubConnection,
            IMessagingCenter messagingCenter,
            string groupName,
            string formatDate)
        {
            this.messagingCenter = messagingCenter;
            this.groupName = groupName;
            this.hubConnection = hubConnection;
            this.formatDate = formatDate;
        }

        public async Task<long> RunAsync()
        {
            var rsp = await messagingCenter.Send(new SendMessage<NotificationContent>
            {
                Nonce = Guid.NewGuid().ToString(),
                TargetGroups = new[] { groupName },
                Filter = new MessageFilter
                {
                    Scopes = new[] { "scope1", "scope2" },
                    Activities = new[] { "act1", "act2" },
                },
                Content = new NotificationContent
                {
                    EndpointUrl = "https://www.google.com",
                    Message = "msg1",
                },
            });

            if (!string.IsNullOrWhiteSpace(rsp.ErrorMessage))
            {
                Console.WriteLine($"Send fail, reason: {rsp.ErrorMessage}");
                return 0;
            }

            Console.WriteLine($"{DateTime.Now.ToString(formatDate)} ----------- Send");
            Console.WriteLine($"Send message success");
            
            return 0;
        }
    }
}
