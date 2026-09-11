using Mapster;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests;

/// <summary>
/// Flattens the <see cref="LeaveRequest.Period"/> value object back onto the flat
/// <c>StartDate</c>/<c>EndDate</c> the HTTP contract (<see cref="LeaveRequestDto"/>) still exposes.
/// Mapster's default flattening would otherwise look for <c>PeriodStart</c>/<c>PeriodEnd</c>.
/// Registered once from <c>LeaveManagementModule</c> into Mapster's global config, which both
/// <c>Adapt</c> and <c>ProjectToType</c> use.
/// </summary>
internal static class LeaveRequestMappingConfig
{
    public static void Register()
    {
        TypeAdapterConfig<LeaveRequest, LeaveRequestDto>
            .NewConfig()
            .Map(dest => dest.StartDate, src => src.Period.Start)
            .Map(dest => dest.EndDate, src => src.Period.End);
    }
}
