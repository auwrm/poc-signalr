namespace TTSS.Infrastructure.Services
{
    public interface IDateTimeService
    {
        DateTime UtcNow { get; }

        string GetNumericDateTimeString(DateTime dateTime);
        DateTime ParseNumericDateTime(string numDateTime);
    }
}
