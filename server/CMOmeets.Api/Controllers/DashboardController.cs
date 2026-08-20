using CMOmeets.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/dashboard")]
public class DashboardController(DashboardService svc) : ApiControllerBase
{
    [HttpGet("counters")] public async Task<IActionResult> Counters([FromQuery] long? groupId, [FromQuery] int? deptId, [FromQuery] int? officerId) => Ok(await svc.GetCountersAsync(groupId, Scope(deptId, officerId)));
    [HttpGet("overdue")] public async Task<IActionResult> Overdue([FromQuery] long? groupId, [FromQuery] int? deptId, [FromQuery] int? officerId) => Ok(await svc.GetOverduePointsAsync(groupId, Scope(deptId, officerId)));
    [HttpGet("today-due")] public async Task<IActionResult> TodayDue([FromQuery] long? groupId, [FromQuery] int? deptId, [FromQuery] int? officerId) => Ok(await svc.GetTodayDuePointsAsync(groupId, Scope(deptId, officerId)));
    [HttpGet("upcoming-due")] public async Task<IActionResult> UpcomingDue([FromQuery] long? groupId, [FromQuery] int? deptId, [FromQuery] int? officerId) => Ok(await svc.GetUpcomingDuePointsAsync(groupId, Scope(deptId, officerId)));
    [HttpGet("meeting-abstract")] public async Task<IActionResult> Abstract([FromQuery] long? groupId, [FromQuery] int? deptId, [FromQuery] int? officerId) => Ok(await svc.GetMeetingAbstractAsync(groupId, Scope(deptId, officerId)));
    [HttpGet("department-stats")] [Authorize(Roles = "admin,cm")] public async Task<IActionResult> DepartmentStats([FromQuery] long? groupId, [FromQuery] int? deptId) => Ok(await svc.GetDepartmentStatsAsync(groupId, Scope(deptId)));
    [HttpGet("officer-stats")] [Authorize(Roles = "admin")] public async Task<IActionResult> OfficerStats([FromQuery] long? groupId, [FromQuery] int? deptId, [FromQuery] int? officerId) => Ok(await svc.GetOfficerStatsAsync(groupId, Scope(deptId, officerId)));
}
