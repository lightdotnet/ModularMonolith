using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Monolith.Database;

namespace Monolith.Identity.Data;

public class IdentityContextInitialiser(
    ILogger<IdentityContextInitialiser> logger,
    IdentityDbContext context,
    UserManager<User> userManager,
    RoleManager<Role> roleManager)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }

    public async Task TrySeedAsync()
    {
        logger.LogInformation("identity_module seeding data...");

        try
        {
            if (await context.Database.CanConnectAsync())
            {
                await SeedAsync();
                logger.LogInformation("identity_module seed data completed");
            }
            else
            {
                logger.LogError("identity_module cannot connect to DB");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "identity_module seeding data error: {mess}", ex.Message);
            throw;
        }
    }

    public async Task SeedAsync()
    {
        var role = new Role()
        {
            Name = "super",
            Description = "Super Admin role",
        };

        if (roleManager.Roles.All(r => r.Name != role.Name))
        {
            await roleManager.CreateAsync(role);
            logger.LogInformation("Role {name} added", role.Name);
        }

        var superUser = new User()
        {
            UserName = "super",
            FirstName = "Super",
            LastName = "Admin",
        };

        var defaultPassword = "123";

        if (userManager.Users.All(u => u.UserName != superUser.UserName))
        {
            await userManager.CreateAsync(superUser, defaultPassword);

            logger.LogInformation("User {name} added", superUser.UserName);

            await userManager.AddToRolesAsync(superUser, [role.Name!]);

            logger.LogInformation("Assigned role {role} to user {user}", role.Name, superUser.UserName);
        }

        await userManager.CreateAsync(new User
        {
            UserName = "user",
            FirstName = "Normal",
            LastName = "User",
        }, defaultPassword);

        for (var i = 1; i < 50; i++)
        {
            var user = new User()
            {
                UserName = $"user{i}",
                FirstName = $"User",
                LastName = $"00{i}",
            };

            await userManager.CreateAsync(user, defaultPassword);
        }
    }
}
