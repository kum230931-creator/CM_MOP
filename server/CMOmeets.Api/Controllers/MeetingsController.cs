using CMOmeets.Application.Files;
using CMOmeets.Application.Meetings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/meetings")]
public class MeetingsController(MeetingsService svc, AtrFileService files) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get([FromQuery] bool includeInactive = false, [FromQuery] int? deptId = null, [FromQuery] int? officerId = null)
        => Ok(await svc.GetMeetingsAsync(includeInactive, Scope(deptId, officerId)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id, [FromQuery] int? deptId = null, [FromQuery] int? officerId = null)
    {
        var m = await svc.GetMeetingAsync(id, Scope(deptId, officerId));
        return m is null ? NotFound() : Ok(m);
    }

    // Meetings that still need their minutes (action points) uploaded. An officer sees the meetings
    // that still lack a point for THEM; a nodal login sees its department's meetings that still lack a
    // point for any of the department's officers.
    [HttpGet("pending-minutes")]
    [Authorize(Roles = "officer,cmo_officer,nodal")]
    public async Task<IActionResult> PendingMinutes()
    {
        // A cmo_officer has no officer record, so it resolves pending minutes by its department scope,
        // exactly like a nodal login.
        if (IsNodal || IsCmoOfficer)
            return Ok(await svc.GetPendingMinutesForScopeAsync(Scope()));
        return Ok(CurrentOfficerId is int oid
            ? await svc.GetPendingMinutesMeetingsAsync(new[] { oid })
            : new List<PendingMinuteDto>());
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] MeetingSaveDto dto) => Ok(await svc.CreateMeetingAsync(dto, Actor));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] MeetingSaveDto dto)
        => await svc.UpdateMeetingAsync(id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id) => await svc.DeleteMeetingAsync(id) ? NoContent() : NotFound();

    // Upload (or replace) the meeting's Minutes-of-Meeting document. Admin, nodal and officer logins
    // may upload; a nodal/officer may only upload to a meeting they are part of. Authorisation happens
    // BEFORE the file is written so a rejected request never leaves an orphaned file on disk.
    [HttpPost("{id:int}/document")]
    [Authorize(Roles = "admin,nodal,officer,cmo_officer")]
    [RequestSizeLimit(AtrFileService.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(int id, IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        if (file.Length > AtrFileService.MaxBytes)
            return BadRequest(new { message = "File exceeds the 10 MB limit." });
        if (!files.IsAllowed(file.FileName))
            return BadRequest(new { message = "Only JPG, PNG, PDF, DOC and DOCX files are allowed." });

        if (!await svc.MeetingInScopeAsync(id, Scope()))
            return NotFound();

        var stored = await files.SaveAsync(file);
        var previous = await svc.SetMeetingDocumentAsync(id, stored, Scope());

        // Remove the file we just replaced so old minutes don't accumulate on disk.
        if (previous is not null && previous != stored && files.TryResolve(previous, out var oldPath))
            try { System.IO.File.Delete(oldPath); } catch { /* best effort */ }

        return Ok(new { fileName = stored, originalName = file.FileName });
    }

    // View-only, meeting-scoped download of the Minutes document. Any authenticated user who is
    // entitled to the meeting (admin/cm always, nodal/officer only when a member) may open it — the
    // scope check lives in the service, so a bare filename cannot be used to reach out-of-scope files.
    [HttpGet("{id:int}/document")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var name = await svc.GetMeetingDocumentAsync(id, Scope());
        if (name is null) return NotFound();
        return files.TryResolve(name, out var path)
            ? PhysicalFile(path, AtrFileService.ContentType(name))
            : NotFound();
    }
}
