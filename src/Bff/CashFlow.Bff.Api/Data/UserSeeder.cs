using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Bff.Api.Data;

public static class UserSeeder
{
    public const string DefaultAdminEmail = "admin@admin.com";
    public const string DefaultAdminPassword = "Master@123";
    public const string DefaultAdminName = "Administrador";

    public static async Task SeedAsync(BffDbContext db, CancellationToken ct = default)
    {
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail, ct);

        if (admin is null)
        {
            var now = DateTime.UtcNow;
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = DefaultAdminName,
                Email = DefaultAdminEmail,
                PasswordHash = PasswordHasher.Hash(DefaultAdminPassword),
                Role = Roles.Admin,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else if (admin.Role != Roles.Admin)
        {
            admin.Role = Roles.Admin;
            admin.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
