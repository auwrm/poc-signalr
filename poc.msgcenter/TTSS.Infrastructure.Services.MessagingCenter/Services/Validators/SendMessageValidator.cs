using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class SendMessageValidator
    {
        public static bool Validate(this SendMessage target)
        {
            if (!validate(target)) return false;

            var variantOptions = new List<bool>();
            if (target is SendMessage<DynamicContent> dynamic)
            {
                variantOptions.Add(dynamic.Validate());
            }
            if (target is SendMessage<NotificationContent> noti)
            {
                variantOptions.Add(noti.Validate());
            }
            return variantOptions.All(it => it);
        }

        public static bool Validate(this IEnumerable<SendMessage> target)
            => (target?.Any() ?? false) && target.All(Validate);

        public static bool Validate(this SendMessage<DynamicContent> target)
            => validate(target) && target.Content.Validate();

        public static bool Validate(this SendMessage<NotificationContent> target)
            => validate(target) && target.Content.Validate();

        private static bool validate(SendMessage target)
            => !string.IsNullOrWhiteSpace(target?.Nonce)
            && (target?.Filter?.Scopes?.Any() ?? false)
            && (target?.Filter?.Activities?.Any() ?? false)
            && (target?.TargetGroups?.Any() ?? false)
            && (target?.Filter?.Scopes?.All(it => !string.IsNullOrWhiteSpace(it)) ?? false)
            && (target?.Filter?.Activities?.All(it => !string.IsNullOrWhiteSpace(it)) ?? false)
            && (target?.TargetGroups?.All(it => !string.IsNullOrWhiteSpace(it)) ?? false);
    }
}
