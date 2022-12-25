using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class MessageContentValidator
    {
        public static bool Validate(this DynamicContent target)
            => null != target.Data;

        public static bool Validate(this NotificationContent target)
            => !string.IsNullOrWhiteSpace(target.Message)
            && !string.IsNullOrWhiteSpace(target.EndpointUrl);
    }
}
