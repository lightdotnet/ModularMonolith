using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Companies;

namespace StarterKit.Organization.Api.Application.Companies.Commands;

internal sealed record CreateCompanyCommand(CreateCompanyRequest Model) : ICommand<IResult<string>>;

internal class CreateCompanyCommandHandler(OrganizationDbContext context)
    : ICommandHandler<CreateCompanyCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var codeExists = await context.Companies
            .AnyAsync(x => x.Code == model.Code, cancellationToken);

        if (codeExists)
            return Result<string>.Error($"Company code '{model.Code}' already exists.");

        var entity = new Company
        {
            Name = model.Name,
            Code = model.Code,
            TaxCode = model.TaxCode,
            Address = model.Address,
            Phone = model.Phone,
            Email = model.Email,
            Website = model.Website,
            Description = model.Description,
        };

        await context.Companies.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
