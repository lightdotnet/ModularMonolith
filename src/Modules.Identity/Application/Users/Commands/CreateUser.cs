using Light.Mediator;
using Monolith.Identity.Domain.Events;

namespace Monolith.Identity.Application.Users.Commands;

public record CreateUserCommand : CreateUserRequest, ICommand<IResult<string>>;

internal class CreateUserCommandHandler(
    IUserService userService,
    IPublisher publisher)
    : ICommandHandler<CreateUserCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userService.CreateAsync(request).ConfigureAwait(false);

        if (result.Succeeded)
        {
            await publisher.Publish(
                new UserCreatedEvent(result.Data, request.UserName, request.Email),
                cancellationToken);
        }

        return result;
    }
}
