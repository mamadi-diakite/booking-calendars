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
    public void Changes_the_password_hash()
    {
        var user = new User("c@example.com", "Clara", "old-hash", "UTC", TestDates.Now);

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal("passwordHash", Assert.Throws<ArgumentException>(() => user.ChangePassword(" ")).ParamName);
        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void Changes_the_time_zone()
    {
        var user = new User("c@example.com", "Clara", "hash", "UTC", TestDates.Now);

        user.ChangeTimeZone("Asia/Tokyo");

        Assert.Equal("Asia/Tokyo", user.TimeZoneId);
        Assert.Equal("timeZoneId", Assert.Throws<ArgumentException>(() => user.ChangeTimeZone("Tokyo")).ParamName);
        Assert.Equal("Asia/Tokyo", user.TimeZoneId);
    }

    [Theory]
    [InlineData("clara@example.com", true)]
    [InlineData(" Clara@Example.com ", true)]
    [InlineData("clara@", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    public void Checks_an_email_without_throwing(string? email, bool expected)
    {
        Assert.Equal(expected, User.IsValidEmail(email));
    }

    [Fact]
    public void Email_check_applies_the_length_limit()
    {
        var local = new string('a', User.EmailMaxLength - "@example.com".Length + 1);

        Assert.False(User.IsValidEmail(local + "@example.com"));
        Assert.True(User.IsValidEmail(local[1..] + "@example.com"));
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
