using Microsoft.Extensions.Logging;
using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Organization.Contracts.Common;
using StarterKit.Organization.Contracts.Employees;
using StarterKit.Organization.Contracts.OrgUnits;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Organization.Api.Data;

public class OrganizationContextInitialiser(
    ILogger<OrganizationContextInitialiser> logger,
    OrganizationDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }

    public async Task TrySeedAsync()
    {
        logger.LogInformation("organization_module seeding data...");

        try
        {
            if (await context.Database.CanConnectAsync())
            {
                await SeedAsync();
                logger.LogInformation("organization_module seed data completed");
            }
            else
            {
                logger.LogError("organization_module cannot connect to DB");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "organization_module seeding data error: {mess}", ex.Message);
            throw;
        }
    }

    public async Task SeedAsync()
    {
        var acme = await GetOrCreateCompanyAsync("ACME", "Acme Corporation", "https://acme.example.com");

        // A second, deliberately bare company — just enough to demonstrate multi-company scoping.
        await GetOrCreateCompanyAsync("GLBX", "Globex Corporation", "https://globex.example.com");

        var engineering = await GetOrCreateOrgUnitAsync(acme.Id, null, OrgUnitType.Department, "ENG", "Engineering");
        var backend = await GetOrCreateOrgUnitAsync(acme.Id, engineering.Id, OrgUnitType.Team, "ENG-BE", "Backend Team");
        var frontend = await GetOrCreateOrgUnitAsync(acme.Id, engineering.Id, OrgUnitType.Team, "ENG-FE", "Frontend Team");
        await GetOrCreateOrgUnitAsync(acme.Id, null, OrgUnitType.Department, "HR", "Human Resources");

        var intern = await GetOrCreateEmployeeLevelAsync(acme.Id, "INTERN", "Intern", 1);
        var staff = await GetOrCreateEmployeeLevelAsync(acme.Id, "STAFF", "Staff", 2);
        var senior = await GetOrCreateEmployeeLevelAsync(acme.Id, "SENIOR", "Senior", 3);
        var lead = await GetOrCreateEmployeeLevelAsync(acme.Id, "LEAD", "Lead", 4);
        var manager = await GetOrCreateEmployeeLevelAsync(acme.Id, "MANAGER", "Manager", 5);

        var alice = await GetOrCreateEmployeeAsync(acme.Id, "E001", "Alice", "Nguyen", "alice.nguyen@acme.example.com");
        var bob = await GetOrCreateEmployeeAsync(acme.Id, "E002", "Bob", "Tran", "bob.tran@acme.example.com");
        var carol = await GetOrCreateEmployeeAsync(acme.Id, "E003", "Carol", "Le", "carol.le@acme.example.com");
        var dave = await GetOrCreateEmployeeAsync(acme.Id, "E004", "Dave", "Pham", "dave.pham@acme.example.com");
        var erin = await GetOrCreateEmployeeAsync(acme.Id, "E005", "Erin", "Vo", "erin.vo@acme.example.com");
        var frank = await GetOrCreateEmployeeAsync(acme.Id, "E006", "Frank", "Hoang", "frank.hoang@acme.example.com");

        await AssignMembershipIfMissingAsync(alice.Id, engineering.Id, manager.Id, isPrimary: true);
        await AssignMembershipIfMissingAsync(bob.Id, backend.Id, lead.Id, isPrimary: true);
        await AssignMembershipIfMissingAsync(carol.Id, backend.Id, senior.Id, isPrimary: true);
        await AssignMembershipIfMissingAsync(dave.Id, backend.Id, staff.Id, isPrimary: true);
        await AssignMembershipIfMissingAsync(erin.Id, frontend.Id, senior.Id, isPrimary: true);
        await AssignMembershipIfMissingAsync(frank.Id, frontend.Id, intern.Id, isPrimary: true);
    }

    private async Task<Company> GetOrCreateCompanyAsync(string code, string name, string website)
    {
        var existing = await context.Companies.SingleOrDefaultAsync(x => x.Code == code);

        if (existing is not null)
            return existing;

        var company = new Company
        {
            Name = name,
            Code = code,
            Website = website,
            Status = OrganizationStatus.Active,
        };

        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();

        logger.LogInformation("Company {code} added", code);

        return company;
    }

    private async Task<OrgUnit> GetOrCreateOrgUnitAsync(
        string companyId, string? parentId, OrgUnitType type, string code, string name)
    {
        var existing = await context.OrgUnits
            .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code);

        if (existing is not null)
            return existing;

        var unit = new OrgUnit
        {
            CompanyId = companyId,
            ParentId = parentId,
            Type = type,
            Code = code,
            Name = name,
            Status = OrganizationStatus.Active,
        };

        await context.OrgUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        logger.LogInformation("Org unit {code} added", code);

        return unit;
    }

    private async Task<EmployeeLevel> GetOrCreateEmployeeLevelAsync(
        string companyId, string code, string name, int rank)
    {
        var existing = await context.EmployeeLevels
            .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code);

        if (existing is not null)
            return existing;

        var level = new EmployeeLevel
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            Rank = rank,
        };

        await context.EmployeeLevels.AddAsync(level);
        await context.SaveChangesAsync();

        logger.LogInformation("Employee level {code} added", code);

        return level;
    }

    private async Task<Employee> GetOrCreateEmployeeAsync(
        string companyId, string employeeCode, string firstName, string lastName, string email)
    {
        var existing = await context.Employees
            .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.EmployeeCode == employeeCode);

        if (existing is not null)
            return existing;

        var employee = new Employee
        {
            CompanyId = companyId,
            EmployeeCode = employeeCode,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            EmploymentStatus = EmploymentStatus.Active,
            HireDate = DateTimeOffset.UtcNow,
        };

        await context.Employees.AddAsync(employee);
        await context.SaveChangesAsync();

        logger.LogInformation("Employee {code} added", employeeCode);

        return employee;
    }

    private async Task AssignMembershipIfMissingAsync(
        string employeeId, string orgUnitId, string levelId, bool isPrimary)
    {
        var exists = await context.EmployeeOrgUnitMemberships
            .AnyAsync(x => x.EmployeeId == employeeId && x.OrgUnitId == orgUnitId && x.EndDate == null);

        if (exists)
            return;

        await context.EmployeeOrgUnitMemberships.AddAsync(new EmployeeOrgUnitMembership
        {
            EmployeeId = employeeId,
            OrgUnitId = orgUnitId,
            LevelId = levelId,
            IsPrimary = isPrimary,
            StartDate = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();
    }
}
