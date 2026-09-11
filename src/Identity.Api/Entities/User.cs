using Light.Domain;
using Light.Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;

namespace StarterKit.Identity.Api.Entities;

public class User : IdentityUser, IEntity<string>, IAuditable, ISoftDelete
{
    public User() => Id = LightId.NewId();

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public ActiveStatus Status { get; set; } = new();

    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;

    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTimeOffset? Deleted { get; set; }

    public string? DeletedBy { get; set; }

    public void UpdateInfo(string? firstName, string? lastName, string? phoneNumber, string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Email = email;
    }

    public void UpdateStatus(ActiveStatus.State status)
    {
        // only update 2 status
        if (status == ActiveStatus.State.Active || status == ActiveStatus.State.Locked)
            Status.Update(status);
    }

    public void ChangeAuthProvider(AuthProvider provider)
    {
        // authenticate the user via the given provider instead of a local password
        AuthProvider = provider;
    }

    public static User ProvisionFromExternalIdentity(
        string email,
        string? firstName,
        string? lastName,
        AuthProvider provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            AuthProvider = provider,
            Status = new ActiveStatus(ActiveStatus.State.Active),
        };
        // PasswordHash stays null; caller uses UserManager.CreateAsync(user) (no-password overload).
    }

    public void Delete()
    {
        UserName = null;
        FirstName = null;
        LastName = null;
        PhoneNumber = null;
        Email = null;
        PasswordHash = null;
        AuthProvider = AuthProvider.Local;
        Status.Update(ActiveStatus.State.Locked);
    }
}
