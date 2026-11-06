namespace BookingCalendars.Api.IntegrationTests;

public sealed class JwtAccessTokenIssuerTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "issuer",
        Audience = "audience",
        AccessTokenLifetime = TimeSpan.FromMinutes(30),
        SigningKey = Convert.FromBase64String(BookingCalendarsApiFactory.TestSigningKey),
    };

    [Fact]
    public async Task Issued_token_is_valid_and_carries_the_user_claims()
    {
        var user = new User("mila@example.com", "Mila Laurent", "hash", "UTC", DateTime.UtcNow);
        var issuer = new JwtAccessTokenIssuer(Settings, TimeProvider.System);

        var token = issuer.Issue(user);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, Settings.ToValidationParameters());

        Assert.True(result.IsValid);
        Assert.Equal(user.Id.ToString(), result.ClaimsIdentity.FindFirst(JwtClaims.Subject)?.Value);
        Assert.Equal("mila@example.com", result.ClaimsIdentity.FindFirst(JwtClaims.Email)?.Value);
        Assert.Equal("Mila Laurent", result.ClaimsIdentity.Name);
    }

    [Fact]
    public void Expiry_follows_the_configured_lifetime()
    {
        var now = new DateTimeOffset(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
        var issuer = new JwtAccessTokenIssuer(Settings, new FixedTimeProvider(now));
        var user = new User("mila@example.com", "Mila", "hash", "UTC", now.UtcDateTime);

        var token = issuer.Issue(user);

        Assert.Equal(now.UtcDateTime.AddMinutes(30), token.ExpiresAtUtc);
    }

    [Fact]
    public void Rejects_a_missing_user()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtAccessTokenIssuer(Settings, TimeProvider.System).Issue(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void User_id_is_null_without_a_valid_subject(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(JwtClaims.Subject, subject) };

        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity(claims)).GetUserId());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
