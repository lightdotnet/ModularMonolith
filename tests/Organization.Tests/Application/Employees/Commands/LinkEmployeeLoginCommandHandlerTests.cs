using Light.Contracts;
using Moq;
using Organization.Tests.TestSupport;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Organization.Api.Application.Employees.Commands;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Employees;
using Xunit;

namespace Organization.Tests.Application.Employees.Commands;

public class LinkEmployeeLoginCommandHandlerTests
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
        var handler = new LinkEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new LinkEmployeeLoginCommand(employee.Id, new LinkEmployeeLoginRequest { UserId = "user-2" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenUserIdAlreadyLinkedToAnotherEmployee()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        var otherEmployee = new Employee
        {
            CompanyId = company.Id, EmployeeCode = "E2", FirstName = "C", LastName = "D", UserId = "user-1",
        };
        await host.Context.Employees.AddRangeAsync(employee, otherEmployee);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetByIdAsync("user-1"))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = "user-1", UserName = "jane" }));
        var handler = new LinkEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new LinkEmployeeLoginCommand(employee.Id, new LinkEmployeeLoginRequest { UserId = "user-1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldLink_WhenUserExistsAndIsUnlinked()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetByIdAsync("user-1"))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = "user-1", UserName = "jane" }));
        var handler = new LinkEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new LinkEmployeeLoginCommand(employee.Id, new LinkEmployeeLoginRequest { UserId = "user-1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Employees.FindAsync([employee.Id], TestContext.Current.CancellationToken);
        Assert.Equal("user-1", updated!.UserId);
    }

    [Fact]
    public async Task Handle_ShouldReturnFriendlyError_WhenUniqueIndexRejectsTheLink()
    {
        // Arrange
        // The Sqlite test host enforces the unique index on Employee.UserId, so this covers the
        // race the application pre-check cannot: a competing link for the same user account that
        // is not yet visible to the pre-check's query but is flushed by the same SaveChanges.
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        // Tracked-but-unsaved competing link: invisible to the pre-check's DB query, flushed together
        // with this handler's update and therefore rejected by the unique index.
        host.Context.Employees.Add(
            new Employee { CompanyId = company.Id, EmployeeCode = "E2", FirstName = "C", LastName = "D", UserId = "user-1" });
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetByIdAsync("user-1"))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = "user-1", UserName = "jane" }));
        var handler = new LinkEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new LinkEmployeeLoginCommand(employee.Id, new LinkEmployeeLoginRequest { UserId = "user-1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var host = new OrganizationTestHost();
        var company = new Company { Name = "A", Code = "A" };
        await host.Context.Companies.AddAsync(company, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var employee = new Employee { CompanyId = company.Id, EmployeeCode = "E1", FirstName = "A", LastName = "B" };
        await host.Context.Employees.AddAsync(employee, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(s => s.GetByIdAsync("missing"))
            .ReturnsAsync(Result<UserDto>.NotFound("User missing not found"));
        var handler = new LinkEmployeeLoginCommandHandler(host.Context, userServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new LinkEmployeeLoginCommand(employee.Id, new LinkEmployeeLoginRequest { UserId = "missing" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
