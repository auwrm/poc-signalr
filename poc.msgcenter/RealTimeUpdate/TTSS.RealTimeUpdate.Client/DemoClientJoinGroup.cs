using Microsoft.AspNetCore.SignalR.Client;
using System.Data.Common;
using System.Text.Json;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;

namespace TTSS.RealTimeUpdate.Client
{
    internal class DemoClientJoinGroup
    {
        private readonly HubConnection hubConnection;
        private readonly IMessagingCenter messagingCenter;
        private readonly string groupName;
        private readonly string formatDate;

        public DemoClientJoinGroup(HubConnection hubConnection,
            IMessagingCenter messagingCenter,
            string groupName,
            string formatDate)
        {
            this.messagingCenter = messagingCenter;
            this.groupName = groupName;
            this.hubConnection = hubConnection;
            this.formatDate = formatDate;
        }

        public async Task RunAsync()
        {
            var joinGroupTask = new TaskCompletionSource<string>();
            
            hubConnection.On<string>("setClientId", async otp =>
            {
                Console.WriteLine($"{DateTime.Now.ToString(formatDate)} =========== setClientId");
                Console.WriteLine($"Received OTP: {otp}");
                Console.WriteLine($"{DateTime.Now.ToString(formatDate)} =========== JoinGroup");
                var joinResult = await messagingCenter.JoinGroup(new()
                {
                    Secret = otp,
                    GroupName = groupName,
                    Nonce = Guid.NewGuid().ToString(),
                });

                if (!string.IsNullOrWhiteSpace(joinResult.ErrorMessage))
                {
                    Console.WriteLine($"Join group: {groupName}, failed: {joinResult.ErrorMessage}");
                    joinGroupTask.TrySetResult(string.Empty);
                }
                else
                {
                    Console.WriteLine($"Join group: {joinResult.JoinGroupName}, success\n");
                    joinGroupTask.TrySetResult(otp);
                }
                
            });
            hubConnection.On<long, MessageFilter>("update", (eventId, filter) =>
            {
                Console.WriteLine($"{DateTime.Now.ToString(formatDate)} ---------- update");
                var filterTxt = JsonSerializer.Serialize(filter);
                Console.WriteLine($"Got an update EventId: {eventId}, filter: {filterTxt}");
            });

            try
            {
                Console.Write($"{DateTime.Now.ToString(formatDate)} =========== Connect SignalR");
                //if (hubConnection.State == HubConnectionState.Disconnected)
                //{

                //}
                await hubConnection.StartAsync();
                Console.WriteLine(", Connected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($", {ex.Message}");
            }

            //Console.WriteLine($"{DateTime.Now.ToString(formatDate)} =========== joinGroupTask");
            var secret = await joinGroupTask.Task;
            if (string.IsNullOrWhiteSpace(secret))
            {
                Console.WriteLine("Exit");
                return;
            }

            //if (hubConnection.State == HubConnectionState.Connected)
            //{
            //    Console.Write($"{DateTime.Now.ToString(formatDate)} =========== SignalR");
            //    await hubConnection.StopAsync();
            //    Console.WriteLine(", DisConnected \n");
            //}
        }
    }
}
