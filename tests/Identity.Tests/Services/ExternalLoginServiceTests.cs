using Light.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.Services;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.ExternalLogin;
using StarterKit.Identity.Contracts.Users;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Services;

public class ExternalLoginServiceTests
{
    private const string Provider = "Microsoft";
    private const string TenantId = "tenant-1";
    private const string ObjectId = "object-1";
    private const string Email = "jane@contoso.com";
    private const string ExpectedProviderKey = "tenant-1|object-1";

    private static Mock<UserManager<User>> CreateUserManagerMock() =>
        new(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    private static (ExternalLoginService Service, Mock<UserManager<User>> UserManager, Mock<IPublisher> Publisher)
        CreateSut(
            IEnumerable<string>? allowedTenants = null,
            IEnumerable<string>? allowedEmailDomains = null)
    {
        var userManagerMock = CreateUserManagerMock();
        var publisherMock = new Mock<IPublisher>();
        var options = Options.Create(new ExternalLoginOptions
        {
            AllowedTenantIds = (allowedTenants ?? [TenantId]).ToList(),
            AllowedEmailDomains = (allowedEmailDomains ?? ["contoso.com"]).ToList(),
        });

        var service = new ExternalLoginService(
            userManagerMock.Object,
            options,
            publisherMock.Object,
            NullLogger<ExternalLoginService>.Instance);

        return (service, userManagerMock, publisherMock);
    }

    private static ExternalLoginDescriptor Descriptor(
        string provider = Provider,
        string objectId = ObjectId,
        string tenantId = TenantId,
        string? email = Email,
        string? firstName = "Jane",
        string? lastName = "Doe") =>
        new(provider, objectId, tenantId, email, firstName, lastName);

    [Theory]
    [InlineData("", TenantId, Email)]
    [InlineData("   ", TenantId, Email)]
    [InlineData(ObjectId, "", Email)]
    [InlineData(ObjectId, "   ", Email)]
    [InlineData(ObjectId, TenantId, null)]
    [InlineData(ObjectId, TenantId, "   ")]
    public async Task ResolveAsync_ShouldReject_WhenRequiredClaimsAreMissing(
        string objectId,
        string tenantId,
        string? email)
    {
        // Arrange
        var (service, userManager, _) = CreateSut();

        // Act
        var outcome = await service.ResolveAsync(
            Descriptor(objectId: objectId, tenantId: tenantId, email: email),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.MissingRequiredClaims, outcome.Reason);
        userManager.Verify(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReject_WhenTenantIsNotAllowListed()
    {
        // Arrange
        var (service, userManager, _) = CreateSut(allowedTenants: ["other-tenant"]);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.TenantNotAllowed, outcome.Reason);
        userManager.Verify(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldAllowTenant_CaseInsensitively()
    {
        // Arrange
        var (service, userManager, _) = CreateSut(allowedTenants: ["TENANT-1"]);
        var user = new User { UserName = Email };
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync(user);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Linked, outcome.Status);
        Assert.Equal(user.Id, outcome.UserId);
    }

    [Fact]
    public async Task ResolveAsync_ShouldLink_WhenProviderLoginExistsAndUserIsActive()
    {
        // Arrange
        var (service, userManager, publisher) = CreateSut();
        var user = new User { UserName = Email, Status = new ActiveStatus(ActiveStatus.State.Active) };
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync(user);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Linked, outcome.Status);
        Assert.Equal(user.Id, outcome.UserId);
        Assert.Null(outcome.Reason);
        userManager.Verify(m => m.FindByLoginAsync(Provider, ExpectedProviderKey), Times.Once);
        userManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(ActiveStatus.State.Locked, false)]
    [InlineData(ActiveStatus.State.Inactive, false)]
    [InlineData(ActiveStatus.State.Active, true)]
    public async Task ResolveAsync_ShouldReject_WhenLinkedUserIsInactiveOrDeleted(
        ActiveStatus.State status,
        bool deleted)
    {
        // Arrange
        var (service, userManager, _) = CreateSut();
        var user = new User
        {
            UserName = Email,
            Status = new ActiveStatus(status),
            Deleted = deleted ? DateTimeOffset.UtcNow : null,
        };
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync(user);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.UserInactive, outcome.Reason);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReject_WhenEmailAlreadyRegistered_WithoutAutoLinking()
    {
        // Arrange
        var (service, userManager, publisher) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(new User { UserName = Email });

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.EmailAlreadyRegistered, outcome.Reason);
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Never);
        userManager.Verify(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()), Times.Never);
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("jane@evil.com")]
    [InlineData("jane@sub.contoso.com")]
    [InlineData("plainaddress")]
    [InlineData("jane@")]
    public async Task ResolveAsync_ShouldReject_WhenEmailDomainIsNotAllowListed(string email)
    {
        // Arrange
        var (service, userManager, _) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(email: email), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.EmailDomainNotAllowed, outcome.Reason);
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldProvisionAndPublish_OnHappyPath()
    {
        // Arrange
        var (service, userManager, publisher) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((User?)null);

        User? created = null;
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => created = u)
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Provisioned, outcome.Status);
        Assert.Null(outcome.Reason);
        Assert.NotNull(created);
        Assert.Equal(created!.Id, outcome.UserId);
        Assert.Equal(Email, created.Email);
        Assert.Equal(AuthProvider.EntraId, created.AuthProvider);
        Assert.True(created.EmailConfirmed);

        userManager.Verify(
            m => m.AddLoginAsync(
                It.Is<User>(u => u.Id == created.Id),
                It.Is<UserLoginInfo>(l => l.LoginProvider == Provider && l.ProviderKey == ExpectedProviderKey)),
            Times.Once);
        userManager.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
        publisher.Verify(
            p => p.Publish(
                It.Is<ExternalUserProvisionedIntegrationEvent>(e =>
                    e.UserId == created.Id
                    && e.Email == Email
                    && e.FirstName == "Jane"
                    && e.LastName == "Doe"
                    && e.Provider == AuthProvider.EntraId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldPassProviderKeyInTenantPipeObjectFormat()
    {
        // Arrange
        var (service, userManager, _) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        userManager.Verify(m => m.FindByLoginAsync(Provider, ExpectedProviderKey), Times.Once);
        userManager.Verify(
            m => m.AddLoginAsync(
                It.IsAny<User>(),
                It.Is<UserLoginInfo>(l => l.ProviderKey == ExpectedProviderKey)),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReject_WhenUserCreationFails()
    {
        // Arrange
        var (service, userManager, publisher) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((User?)null);
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "taken" }));

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.ProvisioningFailed, outcome.Reason);
        userManager.Verify(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()), Times.Never);
        userManager.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldRollBackProvisionedUser_WhenLoginLinkingFails()
    {
        // Arrange
        var (service, userManager, publisher) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((User?)null);

        User? created = null;
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => created = u)
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "LoginAlreadyAssociated", Description = "x" }));
        userManager
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.ProvisioningFailed, outcome.Reason);
        Assert.NotNull(created);
        userManager.Verify(m => m.DeleteAsync(It.Is<User>(u => u.Id == created!.Id)), Times.Once);
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldStillReject_WhenRollbackDeleteAlsoFails()
    {
        // Arrange
        var (service, userManager, _) = CreateSut();
        userManager.Setup(m => m.FindByLoginAsync(Provider, ExpectedProviderKey)).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((User?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "LoginAlreadyAssociated", Description = "x" }));
        userManager
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "y" }));

        // Act
        var outcome = await service.ResolveAsync(Descriptor(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExternalLoginStatus.Rejected, outcome.Status);
        Assert.Equal(ExternalLoginRejectionReason.ProvisioningFailed, outcome.Reason);
    }
}
