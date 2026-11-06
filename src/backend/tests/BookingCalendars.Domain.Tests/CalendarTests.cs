namespace BookingCalendars.Domain.Tests;

public sealed class CalendarTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void Creates_a_calendar_with_its_meeting_lengths_sorted()
    {
        var calendar = new Calendar(
            OwnerId, " Intro call ", " A first talk. ", " Video link sent by message ", "intro-call",
            [new(60, 15, 15), new(15, 5, 5)], TestDates.Now);

        Assert.NotEqual(Guid.Empty, calendar.Id);
        Assert.Equal(OwnerId, calendar.OwnerId);
        Assert.Equal("Intro call", calendar.Title);
        Assert.Equal("A first talk.", calendar.Description);
        Assert.Equal("Video link sent by message", calendar.LocationNote);
        Assert.Equal("intro-call", calendar.Slug);
        Assert.False(calendar.IsPaused);
        Assert.Equal(TestDates.Now, calendar.CreatedAtUtc);
        Assert.Empty(calendar.WeeklyHours);
        Assert.Equal([15, 60], calendar.MeetingLengths.Select(m => m.Minutes));
        Assert.All(calendar.MeetingLengths, m => Assert.Equal(calendar.Id, m.CalendarId));
        var hour = calendar.MeetingLengths.Last();
        Assert.Equal((15, 15), (hour.BufferBeforeMinutes, hour.BufferAfterMinutes));
    }

    [Fact]
    public void Description_and_location_note_are_optional()
    {
        var calendar = NewCalendar(description: " ", locationNote: null);

        Assert.Null(calendar.Description);
        Assert.Null(calendar.LocationNote);
    }

    [Fact]
    public void Setting_meeting_lengths_keeps_the_lengths_still_offered()
    {
        var calendar = NewCalendar(meetingLengths: [new(15, 0, 0), new(30, 0, 0)]);
        var thirty = calendar.MeetingLengths.Single(m => m.Minutes == 30);

        calendar.SetMeetingLengths([new(30, 10, 5), new(60, 0, 0)]);

        Assert.Equal([30, 60], calendar.MeetingLengths.Select(m => m.Minutes));
        Assert.Same(thirty, calendar.MeetingLengths.First());
        Assert.Equal((10, 5), (thirty.BufferBeforeMinutes, thirty.BufferAfterMinutes));
    }

    [Fact]
    public void Buffers_accept_zero_and_the_maximum()
    {
        var calendar = NewCalendar(meetingLengths: [new(30, 0, MeetingLengths.MaxBufferMinutes)]);

        Assert.Equal(MeetingLengths.MaxBufferMinutes, calendar.MeetingLengths.Single().BufferAfterMinutes);
    }

    [Fact]
    public void Rejects_invalid_meeting_lengths()
    {
        Assert.Throws<ArgumentNullException>(() => NewCalendar().SetMeetingLengths(null!));
        Assert.Equal("meetingLengths", Assert.Throws<ArgumentException>(() => NewCalendar(meetingLengths: [])).ParamName);
        Assert.Equal("meetingLengths", Assert.Throws<ArgumentException>(
            () => NewCalendar(meetingLengths: [new(30, 0, 0), new(30, 5, 5)])).ParamName);
        Assert.Equal("meetingLengths", Assert.Throws<ArgumentOutOfRangeException>(
            () => NewCalendar(meetingLengths: [new(45, 0, 0)])).ParamName);
        Assert.Equal("meetingLengths", Assert.Throws<ArgumentOutOfRangeException>(
            () => NewCalendar(meetingLengths: [new(30, -1, 0)])).ParamName);
        Assert.Equal("meetingLengths", Assert.Throws<ArgumentOutOfRangeException>(
            () => NewCalendar(meetingLengths: [new(30, 0, MeetingLengths.MaxBufferMinutes + 1)])).ParamName);
    }

    [Fact]
    public void Setting_weekly_hours_replaces_them_sorted_by_day_and_time()
    {
        var calendar = NewCalendar();
        calendar.SetWeeklyHours([WeeklyHoursRulesTests.Range(DayOfWeek.Friday, 9, 10)]);

        calendar.SetWeeklyHours(
        [
            WeeklyHoursRulesTests.Range(DayOfWeek.Tuesday, 14, 17),
            WeeklyHoursRulesTests.Range(DayOfWeek.Monday, 9, 12),
            WeeklyHoursRulesTests.Range(DayOfWeek.Tuesday, 9, 12),
        ]);

        Assert.Equal(
            [(DayOfWeek.Monday, 9), (DayOfWeek.Tuesday, 9), (DayOfWeek.Tuesday, 14)],
            calendar.WeeklyHours.Select(r => (r.Day, r.Start.Hour)));
        Assert.All(calendar.WeeklyHours, r => Assert.Equal(calendar.Id, r.CalendarId));
    }

    [Fact]
    public void Rejects_invalid_weekly_hours_and_keeps_the_old_ones()
    {
        var calendar = NewCalendar();
        calendar.SetWeeklyHours([WeeklyHoursRulesTests.Range(DayOfWeek.Friday, 9, 10)]);

        Assert.Throws<ArgumentNullException>(() => calendar.SetWeeklyHours(null!));
        var error = Assert.Throws<ArgumentException>(() => calendar.SetWeeklyHours(
            [WeeklyHoursRulesTests.Range(DayOfWeek.Monday, 9, 12), WeeklyHoursRulesTests.Range(DayOfWeek.Monday, 11, 13)]));

        Assert.Equal("ranges", error.ParamName);
        Assert.StartsWith("Monday: 09:00-12:00 and 11:00-13:00 overlap.", error.Message);
        Assert.Equal(DayOfWeek.Friday, Assert.Single(calendar.WeeklyHours).Day);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("ownerId", Assert.Throws<ArgumentException>(() => NewCalendar(ownerId: Guid.Empty)).ParamName);
        Assert.Equal("title", Assert.Throws<ArgumentException>(() => NewCalendar(title: " ")).ParamName);
        Assert.Equal("description", Assert.Throws<ArgumentException>(
            () => NewCalendar(description: new string('d', Calendar.DescriptionMaxLength + 1))).ParamName);
        Assert.Equal("locationNote", Assert.Throws<ArgumentException>(
            () => NewCalendar(locationNote: new string('l', Calendar.LocationNoteMaxLength + 1))).ParamName);
        Assert.Equal("slug", Assert.Throws<ArgumentException>(() => NewCalendar(slug: "Intro Call")).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => NewCalendar(createdAtUtc: DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Unspecified))).ParamName);
    }

    [Fact]
    public void Lists_the_allowed_meeting_lengths()
    {
        Assert.Equal([15, 30, 60], MeetingLengths.AllowedMinutes);
        Assert.True(MeetingLengths.IsAllowed(30));
        Assert.False(MeetingLengths.IsAllowed(45));
    }

    internal static Calendar NewCalendar(
        Guid? ownerId = null,
        string title = "Intro call",
        string? description = null,
        string? locationNote = null,
        string slug = "intro-call",
        MeetingLengthSettings[]? meetingLengths = null,
        DateTime? createdAtUtc = null) =>
        new(ownerId ?? OwnerId, title, description, locationNote, slug,
            meetingLengths ?? [new(30, 0, 0)], createdAtUtc ?? TestDates.Now);
}
