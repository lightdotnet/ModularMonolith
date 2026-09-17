using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Identity.Contracts.Users;

namespace StarterKit.Identity.Api.Application.Users.Commands;

internal sealed record CreateUserCommand(CreateUserRequest Model) : ICommand<IResult<string>>;

internal class CreateUserCommandHandler(
    IUserService userService,
    IPublisher publisher)
    : ICommandHandler<CreateUserCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userService
            .CreateAsync(request.Model)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await publisher.Publish(
                new UserCreatedIntegrationEvent(
                    result.Data,
                    request.Model.UserName,
                    request.Model.Email),
                cancellationToken);
        }

        return result;
    }
}
