namespace BookingCalendars.Infrastructure.Tests;

public sealed class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_does_not_contain_the_password()
    {
        var hash = _hasher.Hash("DemoPass123!");

        Assert.DoesNotContain("DemoPass123!", hash);
    }

    [Fact]
    public void Same_password_gives_different_hashes()
    {
        Assert.NotEqual(_hasher.Hash("DemoPass123!"), _hasher.Hash("DemoPass123!"));
    }

    [Fact]
    public void Verify_accepts_the_right_password()
    {
        var hash = _hasher.Hash("DemoPass123!");

        Assert.True(_hasher.Verify(hash, "DemoPass123!"));
    }

    [Theory]
    [InlineData("demopass123!")]
    [InlineData("DemoPass123")]
    [InlineData("another password")]
    public void Verify_rejects_a_wrong_password(string password)
    {
        var hash = _hasher.Hash("DemoPass123!");

        Assert.False(_hasher.Verify(hash, password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_rejects_a_missing_password(string? password)
    {
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Hash(password!));
    }

    [Theory]
    [InlineData(null, "DemoPass123!")]
    [InlineData("", "DemoPass123!")]
    [InlineData("hash", null)]
    [InlineData("hash", "")]
    public void Verify_rejects_missing_arguments(string? hash, string? password)
    {
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Verify(hash!, password!));
    }
}
