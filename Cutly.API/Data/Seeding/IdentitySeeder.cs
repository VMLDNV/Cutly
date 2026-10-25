using Microsoft.AspNetCore.Identity;

namespace Cutly.API.Data.Seeding;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    [
        "Customer",
        "BusinessOwner",
        "Employee",
        "Admin"
    ];

    public static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Could not create role '{role}': {errors}");
            }
        }
    }
}
