using System.Text;
using CMOmeets.Application.Agendas;
using CMOmeets.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/reports")]
public class ReportsController(ReportsService reports, AgendasService agendas) : ApiControllerBase
{
    [HttpGet("meetings")]
    public async Task<IActionResult> Meetings([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await reports.GetMeetingsReportAsync(from, to, Scope()));

    [HttpGet("actionable-points")]
    public async Task<IActionResult> ActionablePoints([FromQuery] string? status)
        => Ok(await agendas.GetAllActionablePointsAsync(status, Scope()));

    [HttpGet("actionable-points.csv")]
    public async Task<IActionResult> ActionablePointsCsv([FromQuery] string? status)
    {
        var points = await agendas.GetAllActionablePointsAsync(status, Scope());
        var csv = ReportsService.ToCsv(points);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "actionable-points.csv");
    }
}
