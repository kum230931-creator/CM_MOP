using CMOmeets.Application.Agendas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/agendas")]
public class AgendasController(AgendasService svc) : ApiControllerBase
{
    [HttpGet("by-meeting/{meetingId:int}")]
    public async Task<IActionResult> ByMeeting(int meetingId, [FromQuery] int? deptId = null, [FromQuery] int? officerId = null)
        => Ok(await svc.GetAgendasByMeetingAsync(meetingId, Scope(deptId, officerId)));

    [HttpGet("actionable-points")]
    public async Task<IActionResult> AllPoints([FromQuery] string? status, [FromQuery] int? deptId = null, [FromQuery] int? officerId = null)
        => Ok(await svc.GetAllActionablePointsAsync(status, Scope(deptId, officerId)));

    // Single action point detail (drives the /action-point/:id page).
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOne(long id)
    {
        var a = await svc.GetAgendaAsync(id, Scope());
        return a is null ? NotFound() : Ok(a);
    }

    [HttpPost]
    [Authorize(Roles = "admin,officer,cmo_officer,nodal")]
    public async Task<IActionResult> Create([FromBody] AgendaSaveDto dto)
    {
        try { return Ok(await svc.CreateAgendaAsync(dto, Actor, Scope())); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(long id, [FromBody] AgendaSaveDto dto)
    {
        try { return await svc.UpdateAgendaAsync(id, dto, Scope()) ? NoContent() : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(long id)
        => await svc.DeleteAgendaAsync(id, Scope()) ? NoContent() : NotFound();

    // Whether the concerned officer was called about this point, plus the admin's note on that
    // call. Admin-only to write; every role reads it back on the normal action-point endpoints.
    [HttpPut("{id:long}/officer-call")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetOfficerCall(long id, [FromBody] OfficerCallSaveDto dto)
        => await svc.SetOfficerCallAsync(id, dto) ? NoContent() : NotFound();

    // ----- Action-point view log + due notifications -----
    [HttpGet("my-due")]
    [Authorize(Roles = "nodal,officer,cmo_officer")]
    public async Task<IActionResult> MyDue()
        => Ok(await svc.GetMyDuePointsAsync(Scope()));

    [HttpPost("{agendaId:long}/view")]
    [Authorize(Roles = "nodal")]
    public async Task<IActionResult> RecordView(long agendaId)
        => CurrentDeptId is int d && await svc.RecordViewAsync(agendaId, d, Actor) ? NoContent() : NotFound();

    // ----- Remarks / ATR -----
    [HttpGet("{agendaId:long}/remarks")]
    public async Task<IActionResult> Remarks(long agendaId) => Ok(await svc.GetRemarksAsync(agendaId));

    // Activity timeline for a single action point (creation → opens → ATRs → explanations).
    [HttpGet("{agendaId:long}/activity")]
    public async Task<IActionResult> Activity(long agendaId) => Ok(await svc.GetActivityLogAsync(agendaId));

    [HttpPost("remarks")]
    [Authorize(Roles = "admin,officer,cmo_officer,nodal")]
    public async Task<IActionResult> AddRemark([FromBody] RemarkSaveDto dto)
    {
        try
        {
            var r = await svc.AddRemarkAsync(dto, Actor, Scope());
            return r is null ? NotFound(new { message = "Agenda not found." }) : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("remarks/{id:long}/request-explanation")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RequestExplanation(long id, [FromBody] ExplanationRequestDto dto)
    {
        try { return await svc.RequestExplanationAsync(id, dto.Request, Actor) ? NoContent() : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("remarks/{id:long}/explanation")]
    [Authorize(Roles = "admin,officer,cmo_officer,nodal")]
    public async Task<IActionResult> SubmitExplanation(long id, [FromBody] ExplanationReplyDto dto)
    {
        try { return await svc.SubmitExplanationAsync(id, dto, Actor, Scope()) ? NoContent() : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpDelete("remarks/{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteRemark(long id) => await svc.DeleteRemarkAsync(id) ? NoContent() : NotFound();
}
