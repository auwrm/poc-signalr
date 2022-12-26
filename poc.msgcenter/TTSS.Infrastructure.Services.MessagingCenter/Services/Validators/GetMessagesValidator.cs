using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class GetMessagesValidator
    {
        public static bool Validate(this GetMessages target)
            => !string.IsNullOrWhiteSpace(target?.UserId)
            && !string.IsNullOrWhiteSpace(target?.FromGroup)
            && target.Filter.Validate()
            && target.FromMessageId >= 0;
    }
}
