using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class UpdateMessageTrackerValidator
    {
        public static bool Validate(this UpdateMessageTracker target)
            => !string.IsNullOrWhiteSpace(target.UserId)
            && target.FromMessageId >= 0
            && target.ThruMessageId >= 0;
    }
}
