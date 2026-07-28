namespace ProjetoPizza.Application.Identity;

public sealed record LoginCommand(string Email, string Password);

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AuthenticationResultDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserDto User);

public sealed record UserAdminDto(
    Guid Id,
    string Email,
    string DisplayName,
    string EmployeeCode,
    bool IsActive,
    DateTimeOffset? LastAccessAt,
    IReadOnlyCollection<string> Roles);

public sealed record RoleAdminDto(
    Guid Id,
    string Name,
    IReadOnlyCollection<string> Permissions,
    int UserCount);

public sealed record SaveUserCommand(
    Guid? Id,
    string Email,
    string DisplayName,
    string EmployeeCode,
    string? Phone,
    string? Password,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record SaveRoleCommand(
    Guid? Id,
    string Name,
    IReadOnlyCollection<string> Permissions);

public interface IIdentityAccessService
{
    Task<AuthenticationResultDto?> AuthenticateAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserAdminDto>> ListUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RoleAdminDto>> ListRolesAsync(CancellationToken cancellationToken);
    Task<Guid> SaveUserAsync(SaveUserCommand command, CancellationToken cancellationToken);
    Task<Guid> SaveRoleAsync(SaveRoleCommand command, CancellationToken cancellationToken);
}
