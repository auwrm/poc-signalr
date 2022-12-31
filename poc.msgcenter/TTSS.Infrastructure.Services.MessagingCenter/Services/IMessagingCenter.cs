using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services
{
    public interface IMessagingCenter
    {
        Task<SendMessageResponse?> Send(SendMessage message);
        Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages);
        Task<MessagePack> SyncMessage(GetMessages request);
        Task<MessagePack> GetMoreMessages(GetMessages request);
        Task<bool> UpdateMessageTracker(UpdateMessageTracker request);
        Task<bool> ClearAllMessages(ClearAllMessages request);
    }
}
