namespace BookingCalendars.Domain.Tests;

public sealed class BlockedPeriodTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 12, 24);

    [Fact]
    public void Creates_a_period_on_one_calendar()
    {
        var calendarId = Guid.NewGuid();

        var period = new BlockedPeriod(OwnerId, calendarId, Start, Start.AddDays(2), " Holidays ", TestDates.Now);

        Assert.NotEqual(Guid.Empty, period.Id);
        Assert.Equal(OwnerId, period.OwnerId);
        Assert.Equal(calendarId, period.CalendarId);
        Assert.False(period.AppliesToAllCalendars);
        Assert.Equal(Start, period.StartDate);
        Assert.Equal(Start.AddDays(2), period.EndDate);
        Assert.Equal("Holidays", period.Label);
        Assert.Equal(TestDates.Now, period.CreatedAtUtc);
    }

    [Fact]
    public void A_period_without_calendar_applies_to_all_calendars_and_can_last_one_day()
    {
        var period = new BlockedPeriod(OwnerId, null, Start, Start, null, TestDates.Now);

        Assert.True(period.AppliesToAllCalendars);
        Assert.Null(period.CalendarId);
        Assert.Null(period.Label);
        Assert.Equal(period.StartDate, period.EndDate);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("ownerId", Assert.Throws<ArgumentException>(
            () => new BlockedPeriod(Guid.Empty, null, Start, Start, null, TestDates.Now)).ParamName);
        Assert.Equal("calendarId", Assert.Throws<ArgumentException>(
            () => new BlockedPeriod(OwnerId, Guid.Empty, Start, Start, null, TestDates.Now)).ParamName);
        Assert.Equal("endDate", Assert.Throws<ArgumentException>(
            () => new BlockedPeriod(OwnerId, null, Start, Start.AddDays(-1), null, TestDates.Now)).ParamName);
        Assert.Equal("label", Assert.Throws<ArgumentException>(
            () => new BlockedPeriod(OwnerId, null, Start, Start, new string('x', BlockedPeriod.LabelMaxLength + 1),
                TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new BlockedPeriod(OwnerId, null, Start, Start, null,
                DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local))).ParamName);
    }
}
