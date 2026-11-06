namespace BookingCalendars.Domain.Tests;

public sealed class BlockedPeriodRulesTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CalendarA = Guid.NewGuid();
    private static readonly Guid CalendarB = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 12, 24);

    [Theory]
    [InlineData(-3, -1, false)]
    [InlineData(-3, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(2, 5, true)]
    [InlineData(3, 4, false)]
    [InlineData(-1, 4, true)]
    public void Periods_overlap_when_they_share_a_day(int startOffset, int endOffset, bool expected)
    {
        var existing = Period(CalendarA, 0, 2);

        Assert.Equal(expected, BlockedPeriodRules.Overlap(Candidate(CalendarA, startOffset, endOffset), existing));
    }

    [Fact]
    public void Periods_on_different_calendars_do_not_overlap()
    {
        Assert.False(BlockedPeriodRules.Overlap(Candidate(CalendarB, 0, 2), Period(CalendarA, 0, 2)));
    }

    [Fact]
    public void A_period_on_all_calendars_overlaps_every_calendar()
    {
        Assert.True(BlockedPeriodRules.Overlap(Candidate(null, 1, 1), Period(CalendarA, 0, 2)));
        Assert.True(BlockedPeriodRules.Overlap(Candidate(CalendarB, 1, 1), Period(null, 0, 2)));
        Assert.True(BlockedPeriodRules.Overlap(Candidate(null, 1, 1), Period(null, 0, 2)));
    }

    [Fact]
    public void Finds_the_earliest_overlapping_period()
    {
        var later = Period(null, 10, 12);
        var earlier = Period(CalendarA, 5, 6);
        var other = Period(CalendarB, 0, 20);

        var overlap = BlockedPeriodRules.FindOverlap(Candidate(CalendarA, 0, 11), [later, other, earlier]);

        Assert.Same(earlier, overlap);
        Assert.Null(BlockedPeriodRules.FindOverlap(Candidate(CalendarA, 7, 9), [later, other, earlier]));
    }

    [Fact]
    public void Rejects_missing_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => BlockedPeriodRules.Overlap(Candidate(null, 0, 0), null!));
        Assert.Throws<ArgumentNullException>(() => BlockedPeriodRules.FindOverlap(Candidate(null, 0, 0), null!));
    }

    private static BlockedPeriodSettings Candidate(Guid? calendarId, int startOffset, int endOffset) =>
        new(calendarId, Day.AddDays(startOffset), Day.AddDays(endOffset));

    private static BlockedPeriod Period(Guid? calendarId, int startOffset, int endOffset) =>
        new(OwnerId, calendarId, Day.AddDays(startOffset), Day.AddDays(endOffset), null, TestDates.Now);
}
