using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class MessageFilterValidator
    {
        public static bool Validate(this MessageFilter target)
            => (target?.Scopes?.Any() ?? false)
            && (target?.Activities?.Any() ?? false)
            && (target.Scopes?.All(it => !string.IsNullOrWhiteSpace(it)) ?? false)
            && (target.Activities?.All(it => !string.IsNullOrWhiteSpace(it)) ?? false);
    }
}
