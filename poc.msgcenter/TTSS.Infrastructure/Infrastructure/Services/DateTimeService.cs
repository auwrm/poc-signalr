using System.Globalization;

namespace TTSS.Infrastructure.Services
{
    public class DateTimeService : IDateTimeService
    {
        private const string FormatString = "yyyyMMddHHmmss";
        private static readonly IFormatProvider cultureFormat = CultureInfo.InvariantCulture.DateTimeFormat;

        public DateTime UtcNow => DateTime.UtcNow;

        public string GetNumericDateTimeString(DateTime dateTime)
            => dateTime.ToString(FormatString, cultureFormat);

        public DateTime ParseNumericDateTime(string numDateTime)
            => DateTime.ParseExact(numDateTime, FormatString, cultureFormat);
    }
}
