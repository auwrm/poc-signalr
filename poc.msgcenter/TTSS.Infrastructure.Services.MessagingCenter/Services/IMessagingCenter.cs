using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services
{
    public interface IMessagingCenter
    {
        Task<JoinGroupResponse> JoinGroup(JoinGroupRequest req);
        Task<SendMessageResponse?> Send(SendMessage message);
        Task<SendMessageResponse?> Send(IEnumerable<SendMessage> messages);
        Task<MessagePack> SyncMessage(GetMessages request);
        Task<MessagePack> GetMoreMessages(GetMessages request);
        Task<bool> UpdateMessageTracker(UpdateMessageTracker request);
        Task<bool> ClearAllMessages(ClearAllMessages request);
    }
}
