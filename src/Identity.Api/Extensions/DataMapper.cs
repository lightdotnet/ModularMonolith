using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts;
using System.Linq.Expressions;

namespace StarterKit.Identity.Api.Extensions;

public static class DataMapper
{
    private static readonly Expression<Func<User, UserDto>> UserMapperExpression = s => new UserDto
    {
        Id = s.Id,
        UserName = s.UserName!,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Email = s.Email,
        PhoneNumber = s.PhoneNumber,
        AuthProvider = s.AuthProvider == StarterKit.Identity.Contracts.AuthProvider.ActiveDirectory
            ? AuthProviderWire.ActiveDirectory
            : s.AuthProvider == StarterKit.Identity.Contracts.AuthProvider.EntraId
                ? AuthProviderWire.EntraId
                : null,
        Status = s.Status.Value.ToString(),
        IsDeleted = s.Deleted != null,
    };

    private static readonly Func<User, UserDto> UserMapperCompiled = UserMapperExpression.Compile();

    public static IQueryable<UserDto> MapToDto(this IQueryable<User> query) =>
        query.Select(UserMapperExpression);

    public static UserDto MapToDto(this User user) => UserMapperCompiled(user);

    private static readonly Expression<Func<Role, RoleDto>> RoleMapperExpression = s => new RoleDto
    {
        Id = s.Id,
        Name = s.Name!,
        Description = s.Description,
    };

    private static readonly Func<Role, RoleDto> RoleMapperCompiled = RoleMapperExpression.Compile();

    public static IQueryable<RoleDto> MapToDto(this IQueryable<Role> query) =>
        query.Select(RoleMapperExpression);

    public static RoleDto MapToDto(this Role role) => RoleMapperCompiled(role);
}
