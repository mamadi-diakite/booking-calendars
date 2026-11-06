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

    [Theory]
    [InlineData("Intro call", "intro-call")]
    [InlineData("  Café & Crème: 30 min!  ", "cafe-creme-30-min")]
    [InlineData("Équipe -- Réunion", "equipe-reunion")]
    [InlineData("1:1", "1-1")]
    public void Builds_a_slug_from_a_title(string title, string expected)
    {
        var slug = CalendarSlug.FromTitle(title);

        Assert.Equal(expected, slug);
        Assert.True(CalendarSlug.IsValid(slug));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("!!!")]
    [InlineData("日本語")]
    public void Falls_back_when_the_title_has_no_letters_or_digits(string? title)
    {
        Assert.Equal(CalendarSlug.Fallback, CalendarSlug.FromTitle(title));
    }

    [Fact]
    public void Long_titles_are_cut_without_a_trailing_hyphen()
    {
        var slug = CalendarSlug.FromTitle(new string('a', CalendarSlug.MaxLength - 1) + " bcd");

        Assert.Equal(new string('a', CalendarSlug.MaxLength - 1), slug);
    }

    [Fact]
    public void Adds_a_number_within_the_length_limit()
    {
        Assert.Equal("intro-call-2", CalendarSlug.WithNumber("intro-call", 2));

        var longSlug = new string('a', 56) + "-bcd";
        var numbered = CalendarSlug.WithNumber(longSlug, 12);

        Assert.Equal(new string('a', 56) + "-12", numbered);
        Assert.True(CalendarSlug.IsValid(numbered));
    }

    [Fact]
    public void Rejects_values_over_the_limit()
    {
        Assert.True(CalendarSlug.IsValid(new string('a', CalendarSlug.MaxLength)));
        Assert.False(CalendarSlug.IsValid(new string('a', CalendarSlug.MaxLength + 1)));
    }
}
