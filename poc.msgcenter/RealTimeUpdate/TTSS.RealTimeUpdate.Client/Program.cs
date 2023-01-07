using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models.Configs;

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

const string GroupName = "Test";
var joinGroupTask = new TaskCompletionSource<string>();
hubConnection.On<string>("setClientId", async otp =>
{
    Console.WriteLine($"Received OTP: {otp}");
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
        Console.WriteLine($"Join group: {joinResult.JoinGroupName}, success");
        joinGroupTask.TrySetResult(otp);
    }
});

try
{
    Console.Write($"Connect SignalR: {options.HostFQDN}");
    await hubConnection.StartAsync();
    Console.WriteLine(", Connected");
}
catch (Exception ex)
{
    Console.WriteLine($", {ex.Message}");
}

var secret = await joinGroupTask.Task;
if (string.IsNullOrWhiteSpace(secret))
{
    Console.WriteLine("Exit");
    return;
}

Console.ReadLine();