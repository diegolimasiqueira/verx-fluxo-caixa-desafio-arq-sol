using CashFlow.Bff.Api.Data;
using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.DTOs;
using CashFlow.Bff.Api.Middleware;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Bff.Api.Services;

public class UserAppService(BffDbContext db)
{
    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct = default)
    {
        return await db.Users.AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role, u.CreatedAt, u.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException($"User '{id}' not found.");

        return new UserResponse(user.Id, user.Name, user.Email, user.Role, user.CreatedAt, user.UpdatedAt);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new DomainException($"Email '{email}' is already registered.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = Roles.Merchant,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Name, user.Email, user.Role, user.CreatedAt, user.UpdatedAt);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException($"User '{id}' not found.");

        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != id, ct))
            throw new DomainException($"Email '{email}' is already registered.");

        user.Name = request.Name.Trim();
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Name, user.Email, user.Role, user.CreatedAt, user.UpdatedAt);
    }

    public async Task UpdatePasswordAsync(Guid id, UpdatePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException($"User '{id}' not found.");

        user.PasswordHash = PasswordHasher.Hash(request.Password);
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
