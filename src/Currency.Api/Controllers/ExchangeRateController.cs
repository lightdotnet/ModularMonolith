using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Currencies.Api.Application.ExchangeRates.Commands;
using StarterKit.Currencies.Api.Application.ExchangeRates.Queries;
using StarterKit.Currencies.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;

namespace StarterKit.Currencies.Api.Controllers;

[ApiExplorerSettings(GroupName = "currency")]
[Route("api/v{version:apiVersion}/exchange_rate")]
[MustHavePermission(CurrencyPermissions.Rates.View)]
public class ExchangeRateController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchExchangeRateRequest request)
    {
        return Ok(await Mediator.Send(new SearchExchangeRatesQuery(request)));
    }

    /// <summary>The rate in effect per active foreign currency, as of now unless <paramref name="asOf"/> is given.</summary>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestAsync([FromQuery] DateTimeOffset? asOf)
    {
        return Ok(await Mediator.Send(new GetLatestExchangeRatesQuery(asOf)));
    }

    [HttpPost]
    [MustHavePermission(CurrencyPermissions.Rates.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] RecordExchangeRateRequest request)
    {
        return Ok(await Mediator.Send(new RecordExchangeRateCommand(request, _currentUserId)));
    }
}
