namespace BookingCalendars.Domain.Tests;

public sealed class TimeZonesTests
{
    [Theory]
    [InlineData("UTC")]
    [InlineData("Europe/Paris")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Tokyo")]
    public void Accepts_iana_ids(string id)
    {
        Assert.True(TimeZones.IsValid(id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Mars/Olympus")]
    [InlineData("Romance Standard Time")]
    public void Rejects_blank_unknown_and_windows_ids(string? id)
    {
        Assert.False(TimeZones.IsValid(id));
    }

    [Fact]
    public void Rejects_ids_over_the_limit()
    {
        Assert.False(TimeZones.IsValid(new string('A', TimeZones.IdMaxLength + 1)));
    }

    [Fact]
    public void Default_is_valid()
    {
        Assert.True(TimeZones.IsValid(TimeZones.Default));
    }
}
