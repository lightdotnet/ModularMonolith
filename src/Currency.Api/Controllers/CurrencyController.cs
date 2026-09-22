using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Currencies.Api.Application.Currencies.Commands;
using StarterKit.Currencies.Api.Application.Currencies.Queries;
using StarterKit.Currencies.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;

namespace StarterKit.Currencies.Api.Controllers;

[ApiExplorerSettings(GroupName = "currency")]
[Route("api/v{version:apiVersion}/currency")]
[MustHavePermission(CurrencyPermissions.Currencies.View)]
public class CurrencyController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchCurrencyRequest request)
    {
        return Ok(await Mediator.Send(new SearchCurrenciesQuery(request)));
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetAsync([FromRoute] string code)
    {
        return Ok(await Mediator.Send(new GetCurrencyByCodeQuery(code)));
    }

    [HttpPost]
    [MustHavePermission(CurrencyPermissions.Currencies.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateCurrencyRequest request)
    {
        return Ok(await Mediator.Send(new CreateCurrencyCommand(request)));
    }

    [HttpPut("{code}")]
    [MustHavePermission(CurrencyPermissions.Currencies.Manage)]
    public async Task<IActionResult> PutAsync([FromRoute] string code, [FromBody] UpdateCurrencyRequest request)
    {
        return Ok(await Mediator.Send(new UpdateCurrencyCommand(code, request)));
    }

    [HttpPut("{code}/activate")]
    [MustHavePermission(CurrencyPermissions.Currencies.Manage)]
    public async Task<IActionResult> ActivateAsync([FromRoute] string code)
    {
        return Ok(await Mediator.Send(new ChangeCurrencyStatusCommand(code, Activate: true)));
    }

    [HttpPut("{code}/deactivate")]
    [MustHavePermission(CurrencyPermissions.Currencies.Manage)]
    public async Task<IActionResult> DeactivateAsync([FromRoute] string code)
    {
        return Ok(await Mediator.Send(new ChangeCurrencyStatusCommand(code, Activate: false)));
    }
}
