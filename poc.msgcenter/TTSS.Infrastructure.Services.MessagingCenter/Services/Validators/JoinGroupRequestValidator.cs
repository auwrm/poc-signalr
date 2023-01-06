using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services.Validators
{
    public static class JoinGroupRequestValidator
    {
        public static bool Validate(this JoinGroupRequest target)
            => !string.IsNullOrWhiteSpace(target?.Secret)
            && !string.IsNullOrWhiteSpace(target?.Nonce)
            && !string.IsNullOrWhiteSpace(target?.GroupName);
    }
}
