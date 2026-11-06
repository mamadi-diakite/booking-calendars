namespace BookingCalendars.Application.Tests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("DemoPass123!")]
    [InlineData("abcdefg1")]
    public void Accepts_a_valid_password(string password)
    {
        Assert.Empty(PasswordPolicy.Check(password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Requires_a_password(string? password)
    {
        Assert.Equal(["Password is required."], PasswordPolicy.Check(password));
    }

    [Fact]
    public void Rejects_a_short_password()
    {
        Assert.Contains("Use at least 8 characters.", PasswordPolicy.Check("abc1"));
    }

    [Fact]
    public void Rejects_a_long_password()
    {
        Assert.Contains("Use at most 128 characters.", PasswordPolicy.Check(new string('a', 128) + "1"));
    }

    [Fact]
    public void Needs_a_letter_and_a_digit()
    {
        Assert.Equal(["Use at least one letter."], PasswordPolicy.Check("12345678"));
        Assert.Equal(["Use at least one digit."], PasswordPolicy.Check("abcdefgh"));
    }
}
