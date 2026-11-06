namespace BookingCalendars.Domain.Tests;

public sealed class WeeklyHoursRulesTests
{
    [Fact]
    public void Accepts_no_ranges_and_touching_ranges()
    {
        Assert.Empty(WeeklyHoursRules.Check([]));
        Assert.Empty(WeeklyHoursRules.Check(
        [
            Range(DayOfWeek.Monday, 9, 12),
            Range(DayOfWeek.Monday, 12, 13),
            Range(DayOfWeek.Monday, 14, 17),
            Range(DayOfWeek.Tuesday, 9, 17),
        ]));
    }

    [Fact]
    public void Same_hours_on_different_days_do_not_overlap()
    {
        Assert.Empty(WeeklyHoursRules.Check([Range(DayOfWeek.Monday, 9, 12), Range(DayOfWeek.Friday, 9, 12)]));
    }

    [Fact]
    public void Rejects_empty_and_reversed_ranges()
    {
        var errors = WeeklyHoursRules.Check([Range(DayOfWeek.Monday, 9, 9), Range(DayOfWeek.Sunday, 15, 10)]);

        Assert.Equal(
        [
            "Monday 09:00-09:00: a range must end after it starts.",
            "Sunday 15:00-10:00: a range must end after it starts.",
        ], errors);
    }

    [Fact]
    public void Rejects_overlapping_ranges_of_the_same_day_in_any_order()
    {
        var errors = WeeklyHoursRules.Check(
        [
            Range(DayOfWeek.Wednesday, 11, 13),
            Range(DayOfWeek.Wednesday, 9, 12),
            Range(DayOfWeek.Wednesday, 12, 14),
        ]);

        Assert.Equal(
        [
            "Wednesday: 09:00-12:00 and 11:00-13:00 overlap.",
            "Wednesday: 11:00-13:00 and 12:00-14:00 overlap.",
        ], errors);
    }

    [Fact]
    public void Rejects_unknown_days()
    {
        Assert.Equal(["Day is not valid."], WeeklyHoursRules.Check([Range((DayOfWeek)7, 9, 10)]));
    }

    [Fact]
    public void Rejects_a_missing_list()
    {
        Assert.Throws<ArgumentNullException>(() => WeeklyHoursRules.Check(null!));
    }

    internal static WeeklyHoursSettings Range(DayOfWeek day, int startHour, int endHour) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));
}
