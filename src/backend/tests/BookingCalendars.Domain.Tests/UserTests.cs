namespace BookingCalendars.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Creates_a_user()
    {
        var user = new User(" Clara.Weber@Example.com ", " Clara Weber ", "hash", " Europe/Berlin ", TestDates.Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("clara.weber@example.com", user.Email);
        Assert.Equal("Clara Weber", user.DisplayName);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal("Europe/Berlin", user.TimeZoneId);
        Assert.Equal(TestDates.Now, user.CreatedAtUtc);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("email", Assert.Throws<ArgumentException>(
            () => new User("clara@", "Clara", "hash", "UTC", TestDates.Now)).ParamName);
        Assert.Equal("displayName", Assert.Throws<ArgumentException>(
            () => new User("c@example.com", " ", "hash", "UTC", TestDates.Now)).ParamName);
        Assert.Equal("passwordHash", Assert.Throws<ArgumentException>(
            () => new User("c@example.com", "Clara", "", "UTC", TestDates.Now)).ParamName);
        Assert.Equal("timeZoneId", Assert.Throws<ArgumentException>(
            () => new User("c@example.com", "Clara", "hash", "Nowhere/City", TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new User("c@example.com", "Clara", "hash", "UTC",
                DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local))).ParamName);
    }
}
