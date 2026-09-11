using Light.Contracts;
using Microsoft.EntityFrameworkCore;
using Moq;
using Organization.Tests.TestSupport;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Organization.Api.Application.Employees.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Employees;
using StarterKit.Shared.Constants;
using Xunit;

namespace Organization.Tests.Application.Employees.Commands;

public class CreateEmployeeLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReject_WhenEmployeeAlreadyHasLogin()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee
        {
            CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B", UserId = "existing-user",
        };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userServiceMock = new Mock<IUserService>();
        var handler = new CreateEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLoginCommand(employee.Id, new CreateEmployeeLoginRequest { UserName = "jane" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSetEmployeeUserId_WhenUserServiceSucceeds()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(Result<string>.Success("new-user-id"));
        var handler = new CreateEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLoginCommand(employee.Id, new CreateEmployeeLoginRequest { UserName = "jane" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-user-id", result.Data);
        // AsNoTracking: the link is taken via ExecuteUpdateAsync, which bypasses the change
        // tracker, so a tracked read here would return the stale in-memory null UserId.
        var updated = await host.Context.Employees
            .AsNoTracking()
            .FirstAsync(x => x.Id == employee.Id, TestContext.Current.CancellationToken);
        Assert.Equal("new-user-id", updated.UserId);
        userServiceMock.Verify(
            s => s.SetClaimAsync("new-user-id", ClaimTypeConstants.EmployeeId, employee.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotSetUserId_WhenUserServiceFails()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(Result<string>.Error("Username already taken"));
        var handler = new CreateEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLoginCommand(employee.Id, new CreateEmployeeLoginRequest { UserName = "jane" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        var updated = await host.Context.Employees.FindAsync([employee.Id], TestContext.Current.CancellationToken);
        Assert.Null(updated!.UserId);
    }

    [Fact]
    public async Task Handle_ShouldRollBackTheNewUser_WhenTheEmployeeIsLinkedConcurrently()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(() =>
            {
                // Simulate a concurrent CreateEmployeeLogin winning the link while we mint the user:
                // the conditional UPDATE in the handler must then find no row and compensate.
                employee.UserId = "concurrent-user";
                host.Context.SaveChanges();
                return Result<string>.Success("new-user-id");
            });
        userServiceMock.Setup(s => s.DeleteAsync("new-user-id")).ReturnsAsync(Result.Success());
        var handler = new CreateEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new CreateEmployeeLoginCommand(employee.Id, new CreateEmployeeLoginRequest { UserName = "jane" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(s => s.DeleteAsync("new-user-id"), Times.Once);
        userServiceMock.Verify(
            s => s.SetClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        var persisted = await host.Context.Employees
            .AsNoTracking()
            .FirstAsync(x => x.Id == employee.Id, TestContext.Current.CancellationToken);
        Assert.Equal("concurrent-user", persisted.UserId);
    }
}
