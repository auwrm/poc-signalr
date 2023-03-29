using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;
using TTSS.RealTimeUpdate.Client;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
IRestService restService = new RestService();
MessagingCenterOptions options = config.GetRequiredSection(nameof(MessagingCenterOptions)).Get<MessagingCenterOptions>();
IMessagingCenter messagingCenter = new MessagingCenter(restService, options);

var hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{options.HostFQDN}/api")
                .WithAutomaticReconnect()
                .Build();

const string FormatDate = "MM/dd/yyy HH:mm:ss.fff";
const string GroupName = "Test";

//await new DemoClientJoinGroup(hubConnetion1, messagingCenter, GroupName, FormatDate).RunAsync();
//Console.ReadKey();
//await new DemoClientJoinGroup(hubConnection2, messagingCenter, GroupName, FormatDate).RunAsync();
//Console.ReadKey();
//var eventId = await new DemoClientSentMessage(hubConnetion1, messagingCenter, GroupName, FormatDate).RunAsync();
//Console.ReadKey();

var joinGroupTask = new TaskCompletionSource<string>();
hubConnection.On<string>("setClientId", async otp =>
{
    Console.WriteLine($"{DateTime.Now.ToString(FormatDate)} =========== setClientId");
    Console.WriteLine($"Received OTP: {otp}");
    Console.WriteLine($"{DateTime.Now.ToString(FormatDate)} =========== JoinGroup");
    var joinResult = await messagingCenter.JoinGroup(new()
    {
        Secret = otp,
        GroupName = GroupName,
        Nonce = Guid.NewGuid().ToString(),
    });

    if (!string.IsNullOrWhiteSpace(joinResult.ErrorMessage))
    {
        Console.WriteLine($"Join group: {GroupName}, failed: {joinResult.ErrorMessage}");
        joinGroupTask.TrySetResult(string.Empty);
    }
    else
    {
        Console.WriteLine($"Join group: {joinResult.JoinGroupName}, success\n");
        joinGroupTask.TrySetResult(otp);
    }

});

long _eventId = 0;
hubConnection.On<long, MessageFilter>("update", (eventId, filter) =>
{
    Console.WriteLine($"{DateTime.Now.ToString(FormatDate)} ---------- update");
    var filterTxt = JsonSerializer.Serialize(filter);
    Console.WriteLine($"Got an update EventId: {eventId}, filter: {filterTxt}");
    _eventId = eventId;
});

try
{
    Console.Write($"{DateTime.Now.ToString(FormatDate)} =========== Connect SignalR");
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

var rsp = await messagingCenter.Send(new SendMessage<NotificationContent>
{
    Nonce = Guid.NewGuid().ToString(),
    TargetGroups = new[] { GroupName },
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
    return;
}

Console.WriteLine($"{DateTime.Now.ToString(FormatDate)} ----------- Send");
Console.WriteLine($"Send message success\n");

Console.ReadKey();
// Todo: implement
var getMsg = await messagingCenter.SyncMessage(new GetMessages
{
    FromMessageId = _eventId,
    Filter = new MessageFilter
    {
        Scopes = new[] { "scope1", "scope2" },
        Activities = new[] { "act1", "act2" },
    },
    UserId = Guid.NewGuid().ToString(),
    FromGroup = GroupName,
});
Console.WriteLine($"Get Message: {getMsg}\n");

var moreMsg = await messagingCenter.GetMoreMessages(new GetMessages
{
    FromMessageId = _eventId,
    Filter = new MessageFilter
    {
        Scopes = new[] { "scope1", "scope2" },
        Activities = new[] { "act1", "act2" },
    },
    UserId = Guid.NewGuid().ToString(),
    FromGroup = GroupName,
});
Console.WriteLine($"Get More Message: {moreMsg}\n");

var updateMsg = await messagingCenter.UpdateMessageTracker(new UpdateMessageTracker
{
    UserId = Guid.NewGuid().ToString(),
    FromMessageId = 0,
    ThruMessageId = _eventId,
});
Console.WriteLine($"Update Message: {updateMsg}\n");