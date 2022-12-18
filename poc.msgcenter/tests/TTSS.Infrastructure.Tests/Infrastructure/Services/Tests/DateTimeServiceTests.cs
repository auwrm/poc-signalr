using FluentAssertions;

namespace TTSS.Infrastructure.Services.Tests
{
    public class DateTimeServiceTests
    {
        private readonly IDateTimeService sut;

        public DateTimeServiceTests()
            => sut = new DateTimeService();

        [Fact]
        public void GetUtcNow_ShouldBe_UtcFormat()
            => sut.UtcNow.Kind.Should().Be(DateTimeKind.Utc);

        [Fact]
        public void GetNumberDateTimeString_FromUTC()
        {
            const string expected = "20221215070809";
            sut.GetNumericDateTimeString(new DateTime(2022, 12, 15, 7, 8, 9, DateTimeKind.Utc)).Should().Be(expected);
        }

        [Fact]
        public void ParseNumbericDateTime_FromUTC()
        {
            var expected = new DateTime(2022, 12, 15, 7, 8, 9, DateTimeKind.Utc).ToUniversalTime();
            sut.ParseNumericDateTime("20221215070809").Should().Be(expected);
        }
    }
}
