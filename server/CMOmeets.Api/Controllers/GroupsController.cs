using CMOmeets.Application.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/groups")]
[Authorize(Roles = "admin")]
public class GroupsController(GroupsService svc) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await svc.GetGroupsAsync());

    [HttpPost] public async Task<IActionResult> Create([FromBody] GroupSaveDto dto) => Ok(await svc.CreateGroupAsync(dto, Actor));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] GroupSaveDto dto)
        => await svc.UpdateGroupAsync(id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) => await svc.DeleteGroupAsync(id) ? NoContent() : NotFound();

    [HttpGet("{id:long}/meetings")]
    public async Task<IActionResult> Meetings(long id) => Ok(await svc.GetGroupMeetingsAsync(id));

    [HttpPut("{id:long}/meetings")]
    public async Task<IActionResult> SetMeetings(long id, [FromBody] MapMeetingsDto dto)
    {
        await svc.SetGroupMeetingsAsync(id, dto.MeetingIds, Actor);
        return NoContent();
    }
}
