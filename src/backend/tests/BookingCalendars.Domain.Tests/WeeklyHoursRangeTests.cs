namespace BookingCalendars.Domain.Tests;

public sealed class WeeklyHoursRangeTests
{
    private static readonly Guid CalendarId = Guid.NewGuid();

    [Fact]
    public void Creates_a_range()
    {
        var range = new WeeklyHoursRange(CalendarId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 30));

        Assert.NotEqual(Guid.Empty, range.Id);
        Assert.Equal(CalendarId, range.CalendarId);
        Assert.Equal(DayOfWeek.Tuesday, range.Day);
        Assert.Equal(new TimeOnly(9, 0), range.Start);
        Assert.Equal(new TimeOnly(12, 30), range.End);
    }

    [Theory]
    [InlineData(9, 9)]
    [InlineData(10, 9)]
    public void Rejects_an_empty_or_reversed_range(int startHour, int endHour)
    {
        var error = Assert.Throws<ArgumentException>(() => new WeeklyHoursRange(
            CalendarId, DayOfWeek.Monday, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0)));

        Assert.Equal("end", error.ParamName);
    }

    [Fact]
    public void Rejects_an_unknown_day()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new WeeklyHoursRange(
            CalendarId, (DayOfWeek)7, new TimeOnly(9, 0), new TimeOnly(10, 0)));

        Assert.Equal("day", error.ParamName);
    }
}
