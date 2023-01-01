using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class ClearAllMessagesValidator
    {
        public static bool Validate(this ClearAllMessages target)
            => !string.IsNullOrWhiteSpace(target?.UserId);
    }
}
