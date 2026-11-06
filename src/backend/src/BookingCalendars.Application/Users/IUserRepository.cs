namespace BookingCalendars.Application.Users;

public interface IUserRepository
{
    /// <param name="email">Normalized email: trimmed and lowercase.</param>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
