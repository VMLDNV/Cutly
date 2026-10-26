
using Cutly.API.Models.Users;
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

    public static async Task SeedTestBusinessOwnerAsync(
        UserManager<ApplicationUser> userManager,
        string email)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            throw new InvalidOperationException(
                $"Test user '{email}' was not found.");
        }

        if (await userManager.IsInRoleAsync(user, "BusinessOwner"))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(
            user,
            "BusinessOwner");

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Could not assign BusinessOwner role: {errors}");
        }
    }
}
