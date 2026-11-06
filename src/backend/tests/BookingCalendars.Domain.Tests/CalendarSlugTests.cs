namespace BookingCalendars.Domain.Tests;

public sealed class CalendarSlugTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("intro-call")]
    [InlineData("coaching-30-min")]
    public void Accepts_lowercase_words_joined_by_hyphens(string value)
    {
        Assert.True(CalendarSlug.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Intro-call")]
    [InlineData("intro call")]
    [InlineData("intro_call")]
    [InlineData("café")]
    [InlineData("-intro")]
    [InlineData("intro-")]
    [InlineData("intro--call")]
    public void Rejects_other_values(string? value)
    {
        Assert.False(CalendarSlug.IsValid(value));
    }

    [Fact]
    public void Rejects_values_over_the_limit()
    {
        Assert.True(CalendarSlug.IsValid(new string('a', CalendarSlug.MaxLength)));
        Assert.False(CalendarSlug.IsValid(new string('a', CalendarSlug.MaxLength + 1)));
    }
}
