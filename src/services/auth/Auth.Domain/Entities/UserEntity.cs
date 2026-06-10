namespace Auth.Domain.Entities;

public class UserEntity
{
    private UserEntity()
    {
    }

    public UserEntity(string email, string? name)
    {
        Id = Guid.NewGuid();
        SetEmail(email);
        Name = NormalizeName(name);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        Touch();
    }

    public void UpdateName(string? name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Email = email.Trim();
        NormalizedEmail = NormalizeEmail(email);
    }

    private static string? NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
