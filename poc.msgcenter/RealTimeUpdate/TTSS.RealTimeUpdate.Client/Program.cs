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

Console.WriteLine(options.HostUrl);
var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{options.HostUrl}/api")
    .WithAutomaticReconnect()
    .Build();

try
{
    await hubConnection.StartAsync();
    Console.WriteLine("Connected");

    var joinResult = await messagingCenter.JoinGroup(new()
    {
        GroupName = "Test",
        Nonce = Guid.NewGuid().ToString(),
        Secret = hubConnection.ConnectionId,
    });

    if (!string.IsNullOrWhiteSpace(joinResult.ErrorMessage))
    {
        Console.WriteLine($"Join failed: {joinResult.ErrorMessage}");
        return;
    }

    Console.WriteLine($"Join group: {joinResult.JoinGroupName}");
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

Console.ReadLine();