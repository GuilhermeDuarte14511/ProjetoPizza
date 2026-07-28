using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjetoPizza.Application.Identity;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.SharedKernel;
using ProjetoPizza.Infrastructure.Persistence;

namespace ProjetoPizza.Infrastructure.Identity;

public sealed class IdentityAccessService(
    UserManager<IdentityUser<Guid>> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ProjetoPizzaDbContext context,
    IConfiguration configuration) : IIdentityAccessService
{
    private static readonly string[] AllowedPermissions =
    [
        "admin:read",
        "admin:write",
        "operations:read",
        "operations:write"
    ];

    public async Task<AuthenticationResultDto?> AuthenticateAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, command.Password))
        {
            return null;
        }

        var employee = context.Employees.SingleOrDefault(item => item.IdentityUserId == user.Id && item.IsActive);
        if (employee is null)
        {
            return null;
        }

        employee.RegisterAccess();
        await context.SaveChangesAsync(cancellationToken);
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsAsync(user, roles);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetAccessTokenMinutes());
        var accessToken = CreateAccessToken(user, employee, roles, permissions, expiresAt);
        return new AuthenticationResultDto(
            accessToken,
            expiresAt,
            new AuthenticatedUserDto(user.Id, user.Email!, employee.DisplayName, roles.ToArray(), permissions));
    }

    public async Task<IReadOnlyCollection<UserAdminDto>> ListUsersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var employees = context.Employees.OrderBy(employee => employee.DisplayName).ToArray();
        var users = userManager.Users.ToDictionary(user => user.Id);
        var result = new List<UserAdminDto>(employees.Length);
        foreach (var employee in employees)
        {
            if (!users.TryGetValue(employee.IdentityUserId, out var user))
            {
                continue;
            }

            result.Add(new UserAdminDto(
                user.Id,
                user.Email ?? employee.Email,
                employee.DisplayName,
                employee.EmployeeCode,
                employee.IsActive,
                employee.LastAccessAt,
                (await userManager.GetRolesAsync(user)).ToArray()));
        }

        return result;
    }

    public async Task<IReadOnlyCollection<RoleAdminDto>> ListRolesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = userManager.Users.ToArray();
        var result = new List<RoleAdminDto>();
        foreach (var role in roleManager.Roles.OrderBy(role => role.Name).ToArray())
        {
            var claims = await roleManager.GetClaimsAsync(role);
            var userCount = 0;
            foreach (var user in users)
            {
                if (await userManager.IsInRoleAsync(user, role.Name!))
                {
                    userCount++;
                }
            }

            result.Add(new RoleAdminDto(
                role.Id,
                role.Name ?? string.Empty,
                claims.Where(claim => claim.Type == "permission").Select(claim => claim.Value).Order().ToArray(),
                userCount));
        }

        return result;
    }

    public async Task<Guid> SaveUserAsync(SaveUserCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedRoles = command.Roles.Select(role => role.Trim()).Where(role => role.Length > 0).Distinct().ToArray();
        foreach (var role in normalizedRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                throw new BusinessRuleException("identity.role", $"Role '{role}' does not exist.");
            }
        }

        IdentityUser<Guid> user;
        Employee employee;
        if (command.Id.HasValue)
        {
            user = await userManager.FindByIdAsync(command.Id.Value.ToString())
                ?? throw new BusinessRuleException("identity.user", "User does not exist.");
            employee = context.Employees.Single(item => item.IdentityUserId == user.Id);
            user.Email = command.Email.Trim();
            user.UserName = command.Email.Trim();
            var updateResult = await userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult);
            if (!string.IsNullOrWhiteSpace(command.Password))
            {
                if (await userManager.HasPasswordAsync(user))
                {
                    EnsureSucceeded(await userManager.RemovePasswordAsync(user));
                }

                EnsureSucceeded(await userManager.AddPasswordAsync(user, command.Password));
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.Password))
            {
                throw new BusinessRuleException("identity.password", "A password is required for a new user.");
            }

            user = new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                Email = command.Email.Trim(),
                UserName = command.Email.Trim(),
                EmailConfirmed = true,
                LockoutEnabled = true
            };
            EnsureSucceeded(await userManager.CreateAsync(user, command.Password));
            var unitId = context.RestaurantUnits.Single().Id;
            employee = new Employee(
                EmployeeId.New(),
                unitId,
                user.Id,
                command.DisplayName,
                command.Email,
                command.EmployeeCode);
            context.Employees.Add(employee);
        }

        employee.UpdateProfile(command.DisplayName, command.DisplayName, command.Email, command.Phone);
        if (command.IsActive)
        {
            employee.Activate();
        }
        else
        {
            employee.Deactivate();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, currentRoles));
        }

        if (normalizedRoles.Length > 0)
        {
            EnsureSucceeded(await userManager.AddToRolesAsync(user, normalizedRoles));
        }

        await context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<Guid> SaveRoleAsync(SaveRoleCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var permissions = command.Permissions.Distinct().ToArray();
        if (permissions.Any(permission => !AllowedPermissions.Contains(permission, StringComparer.Ordinal)))
        {
            throw new BusinessRuleException("identity.permission", "Role contains an unsupported permission.");
        }

        IdentityRole<Guid> role;
        if (command.Id.HasValue)
        {
            role = await roleManager.FindByIdAsync(command.Id.Value.ToString())
                ?? throw new BusinessRuleException("identity.role", "Role does not exist.");
            role.Name = command.Name.Trim();
            EnsureSucceeded(await roleManager.UpdateAsync(role));
        }
        else
        {
            role = new IdentityRole<Guid>(command.Name.Trim()) { Id = Guid.NewGuid() };
            EnsureSucceeded(await roleManager.CreateAsync(role));
        }

        foreach (var claim in await roleManager.GetClaimsAsync(role))
        {
            if (claim.Type == "permission")
            {
                EnsureSucceeded(await roleManager.RemoveClaimAsync(role, claim));
            }
        }

        foreach (var permission in permissions)
        {
            EnsureSucceeded(await roleManager.AddClaimAsync(role, new Claim("permission", permission)));
        }

        return role.Id;
    }

    private async Task<string[]> GetPermissionsAsync(IdentityUser<Guid> user, IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var claim in await userManager.GetClaimsAsync(user))
        {
            if (claim.Type == "permission")
            {
                permissions.Add(claim.Value);
            }
        }

        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await roleManager.GetClaimsAsync(role))
            {
                if (claim.Type == "permission")
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return permissions.Order().ToArray();
    }

    private string CreateAccessToken(
        IdentityUser<Guid> user,
        Employee employee,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        DateTimeOffset expiresAt)
    {
        var signingKey = configuration["Authentication:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Authentication:SigningKey must contain at least 32 bytes.");
        }

        var issuer = configuration["Authentication:Issuer"] ?? "ProjetoPizza.Api";
        var audience = configuration["Authentication:Audience"] ?? "projeto-pizza-web";
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? employee.Email),
            new(ClaimTypes.Name, employee.DisplayName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private int GetAccessTokenMinutes()
    {
        var configured = configuration.GetValue<int?>("Authentication:AccessTokenMinutes") ?? 30;
        return Math.Clamp(configured, 5, 120);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new BusinessRuleException(
            "identity.validation",
            string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
