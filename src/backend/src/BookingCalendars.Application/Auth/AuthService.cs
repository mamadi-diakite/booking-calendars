namespace BookingCalendars.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider)
{
    private const string TimeZoneError = "Choose a time zone from the list.";

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["displayName"] = ["Name is required."];
        }
        else if (request.DisplayName.Trim().Length > User.DisplayNameMaxLength)
        {
            errors["displayName"] = [$"Use at most {User.DisplayNameMaxLength} characters."];
        }

        string? email = null;
        if (!User.IsValidEmail(request.Email))
        {
            errors["email"] = ["Enter a valid email address."];
        }
        else
        {
            email = request.Email.Trim().ToLowerInvariant();
            if (await users.FindByEmailAsync(email, cancellationToken) is not null)
            {
                errors["email"] = ["An account already uses this email."];
            }
        }

        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? TimeZones.Default : request.TimeZone.Trim();
        if (!TimeZones.IsValid(timeZone))
        {
            errors["timeZone"] = [TimeZoneError];
        }

        AddNewPasswordErrors(errors, "password", request.Password, request.ConfirmPassword);

        if (errors.Count > 0)
        {
            return RegisterResult.Invalid(errors);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = new User(email!, request.DisplayName, passwordHasher.Hash(request.Password), timeZone, now);
        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);

        return RegisterResult.Registered(CreateSession(user));
    }

    /// <returns><c>null</c> when the email or the password is wrong, without telling which.</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            // Hash anyway, so an unknown email takes as long as a wrong password.
            passwordHasher.Hash(request.Password);
            return null;
        }

        return passwordHasher.Verify(user.PasswordHash, request.Password) ? CreateSession(user) : null;
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);

        return user is null ? null : ToResponse(user);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(request.CurrentPassword) || !passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            errors["currentPassword"] = ["Current password is not correct."];
        }

        AddNewPasswordErrors(errors, "newPassword", request.NewPassword, request.ConfirmPassword);
        if (!errors.ContainsKey("newPassword") && request.NewPassword == request.CurrentPassword)
        {
            errors["newPassword"] = ["New password must be different from the current one."];
        }

        if (errors.Count > 0)
        {
            return ChangePasswordResult.Invalid(errors);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await users.SaveChangesAsync(cancellationToken);

        return ChangePasswordResult.Changed;
    }

    public async Task<ChangeTimeZoneResult> ChangeTimeZoneAsync(
        Guid userId,
        ChangeTimeZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ChangeTimeZoneResult.UserNotFound;
        }

        var timeZone = request.TimeZone?.Trim();
        if (!TimeZones.IsValid(timeZone))
        {
            return ChangeTimeZoneResult.Invalid(new Dictionary<string, string[]> { ["timeZone"] = [TimeZoneError] });
        }

        user.ChangeTimeZone(timeZone);
        await users.SaveChangesAsync(cancellationToken);

        return ChangeTimeZoneResult.Changed(ToResponse(user));
    }

    private static void AddNewPasswordErrors(
        Dictionary<string, string[]> errors,
        string passwordField,
        string password,
        string confirmPassword)
    {
        var policyErrors = PasswordPolicy.Check(password);
        if (policyErrors.Count > 0)
        {
            errors[passwordField] = [.. policyErrors];
        }

        if (confirmPassword != password)
        {
            errors["confirmPassword"] = ["Confirmation does not match the password."];
        }
    }

    private LoginResponse CreateSession(User user)
    {
        var token = tokenIssuer.Issue(user);

        return new LoginResponse(token.Value, token.ExpiresAtUtc, ToResponse(user));
    }

    private static CurrentUserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.TimeZoneId);
}
