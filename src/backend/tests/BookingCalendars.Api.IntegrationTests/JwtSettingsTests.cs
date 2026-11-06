namespace BookingCalendars.Api.IntegrationTests;

public sealed class JwtSettingsTests
{
    [Fact]
    public void Reads_all_values_from_configuration()
    {
        var settings = JwtSettings.FromConfiguration(
            Configuration(("Issuer", "issuer"), ("Audience", "audience"), ("AccessTokenMinutes", "15"),
                ("SigningKey", BookingCalendarsApiFactory.TestSigningKey)));

        Assert.Equal("issuer", settings.Issuer);
        Assert.Equal("audience", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15), settings.AccessTokenLifetime);
        Assert.Equal(Convert.FromBase64String(BookingCalendarsApiFactory.TestSigningKey), settings.SigningKey);
    }

    [Fact]
    public void Uses_defaults_when_only_the_key_is_set()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(("SigningKey", BookingCalendarsApiFactory.TestSigningKey)));

        Assert.Equal("booking-calendars", settings.Issuer);
        Assert.Equal("booking-calendars", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(60), settings.AccessTokenLifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void A_missing_key_stops_startup(string? key)
    {
        var values = key is null ? [] : new[] { ("SigningKey", key) };

        var error = Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(Configuration(values)));

        Assert.Contains("Jwt:SigningKey is missing", error.Message);
    }

    [Theory]
    [InlineData("not base64!", "base64")]
    [InlineData("c2hvcnQ=", "at least 32 bytes")]
    public void Rejects_an_invalid_key(string key, string message)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => JwtSettings.FromConfiguration(Configuration(("SigningKey", key))));

        Assert.Contains(message, error.Message);
    }

    [Fact]
    public void Validation_parameters_match_the_settings()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(("SigningKey", BookingCalendarsApiFactory.TestSigningKey)));

        var parameters = settings.ToValidationParameters();

        Assert.Equal(settings.Issuer, parameters.ValidIssuer);
        Assert.Equal(settings.Audience, parameters.ValidAudience);
        Assert.Equal(JwtClaims.Name, parameters.NameClaimType);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>($"Jwt:{v.Key}", v.Value)))
            .Build();
}
