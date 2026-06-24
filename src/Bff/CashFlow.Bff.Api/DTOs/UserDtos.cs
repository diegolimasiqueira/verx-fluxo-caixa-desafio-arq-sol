using System.ComponentModel.DataAnnotations;

namespace CashFlow.Bff.Api.DTOs;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateUserRequest(
    [Required][MaxLength(120)] string Name,
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MinLength(6)] string Password
);

public record UpdateUserRequest(
    [Required][MaxLength(120)] string Name,
    [Required][EmailAddress][MaxLength(255)] string Email
);

public record UpdatePasswordRequest(
    [Required][MinLength(6)] string Password
);
