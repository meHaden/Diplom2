using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OutfitPlaner_Applcation.Models;

namespace OutfitPlaner_Applcation.Data;

public static class SeedData
{
    public static async Task Initialize(
        WardrobeDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Проверяем, есть ли уже роли в базе 
        if (await roleManager.Roles.AnyAsync())
        {
            return; // База уже инициализирована
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Создание ролей
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Создание администратора
            var adminEmail = "vlasenkoadmin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // Создание соответствующей записи в User
                    var dbUser = new User
                    {
                        UserName = adminUser.UserName,
                        Email = adminUser.Email,
                        PasswordHash = "external_auth",
                        CreatedAt = DateTime.UtcNow
                    };

                    if (context.Database.CanConnect() &&
                        context.Database.GetPendingMigrations().Any())
                    {
                        await context.Database.MigrateAsync();
                    }

                    context.Users.Add(dbUser);
                    await context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Ошибка инициализации базы данных", ex);
        }
    }
}