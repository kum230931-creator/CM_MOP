using CMOmeets.Application.Common;
using CMOmeets.Application.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected string Actor => User.Identity?.Name ?? "system";
    protected string? CurrentRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    // The four roles: admin (full), cm (read-only, all depts), officer (multi-department, view + act),
    // nodal (single department, view + ATR). `sadmin`/`dept` are migrated to admin/nodal at seed time.
    protected bool IsNodal => User.IsInRole("nodal");
    protected bool IsOfficer => User.IsInRole("officer");
    protected bool IsCm => User.IsInRole("cm");
    protected bool IsAdmin => User.IsInRole("admin");
    // A 'cmo_officer' login: scoped to a directly-chosen set of departments (no officer record).
    protected bool IsCmoOfficer => User.IsInRole("cmo_officer");
    // Set for a nodal login — the single department this account is pinned to.
    protected int? CurrentDeptId =>
        int.TryParse(User.FindFirst("deptId")?.Value, out var d) ? d : null;
    // Set for an officer login — the officer rid this account is tied to (drives its department set).
    protected int? CurrentOfficerId =>
        int.TryParse(User.FindFirst("officerId")?.Value, out var o) ? o : null;
    // Set for a 'cmo_officer' login — the department set (departmentMas.RID) this account was given.
    protected List<int> CurrentDeptIds => ScopeResolver.ParseCsvInts(User.FindFirst("deptIds")?.Value);

    // Build the data-scope for a request. `deptId`/`officerId` are the optional drill-down query
    // params: honoured only for admins, and (for deptId) as an officer's title-bar department pick.
    // Admin/CM's advanced title-bar filter (Level + multi-select) is read straight off the query string
    // (`scopeLevel` + repeated/CSV `scopeIds`), so every endpoint that already calls Scope() picks it up.
    protected ScopeRequest Scope(int? deptId = null, int? officerId = null)
    {
        string? scopeLevel = null;
        IReadOnlyList<int>? scopeIds = null;
        if (IsAdmin || IsCm)
        {
            var lvl = Request.Query["scopeLevel"];
            if (lvl.Count > 0 && !string.IsNullOrWhiteSpace(lvl[0])) scopeLevel = lvl[0];

            var raw = Request.Query["scopeIds"];
            if (raw.Count > 0)
            {
                var parsed = raw
                    .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Select(v => int.TryParse(v, out var n) ? n : (int?)null)
                    .Where(n => n.HasValue)
                    .Select(n => n!.Value)
                    .Distinct()
                    .ToList();
                if (parsed.Count > 0) scopeIds = parsed;
            }
        }
        return new ScopeRequest(
            IsAdmin: IsAdmin,
            IsCm: IsCm,
            IsNodal: IsNodal,
            IsOfficer: IsOfficer,
            NodalDeptId: IsNodal ? CurrentDeptId : null,
            OfficerLoginId: IsOfficer ? CurrentOfficerId : null,
            RequestedDeptId: deptId,
            RequestedOfficerId: officerId,
            ScopeLevel: scopeLevel,
            ScopeIds: scopeIds,
            IsCmoOfficer: IsCmoOfficer,
            CmoDeptIds: IsCmoOfficer ? CurrentDeptIds : null);
    }
}

[Route("api/ministries")]
public class MinistriesController(MastersService svc) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await svc.GetMinistriesAsync());

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] MinistrySaveDto dto) => Ok(await svc.CreateMinistryAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] MinistrySaveDto dto)
        => await svc.UpdateMinistryAsync(id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try { return await svc.DeleteMinistryAsync(id) ? NoContent() : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[Route("api/departments")]
public class DepartmentsController(MastersService svc) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get([FromQuery] int? ministryId) => Ok(await svc.GetDepartmentsAsync(ministryId));

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] DepartmentSaveDto dto) => Ok(await svc.CreateDepartmentAsync(dto, Actor));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentSaveDto dto)
        => await svc.UpdateDepartmentAsync(id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id) => await svc.DeleteDepartmentAsync(id) ? NoContent() : NotFound();
}

[Route("api/designations")]
public class DesignationsController(MastersService svc) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get([FromQuery] int? deptId) => Ok(await svc.GetDesignationsAsync(deptId));

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] DesignationSaveDto dto) => Ok(await svc.CreateDesignationAsync(dto, Actor));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] DesignationSaveDto dto)
        => await svc.UpdateDesignationAsync(id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id) => await svc.DeleteDesignationAsync(id) ? NoContent() : NotFound();
}

[Route("api/officers")]
public class OfficersController(MastersService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOfficers(
    [FromQuery] int? deptId,
    [FromQuery] bool onlyAssigned = false)   
    {
        var officers = await svc.GetOfficersAsync(deptId, onlyAssigned);  
        return Ok(officers);
    }
    //public async Task<IActionResult> Get([FromQuery] int? deptId) => Ok(await svc.GetOfficersAsync(deptId));

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] OfficerSaveDto dto)
    {
        try { return Ok(await svc.CreateOfficerAsync(dto, Actor)); }
        catch (DesignationConflictException ex) { return Conflict(new { conflicts = ex.Conflicts }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] OfficerSaveDto dto)
    {
        try { return await svc.UpdateOfficerAsync(id, dto, Actor) ? NoContent() : NotFound(); }
        catch (DesignationConflictException ex) { return Conflict(new { conflicts = ex.Conflicts }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id) => await svc.DeleteOfficerAsync(id) ? NoContent() : NotFound();
}

[Route("api/districts")]
public class DistrictsController(MastersService svc) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await svc.GetDistrictsAsync());

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] DistrictSaveDto dto)
    {
        try { return Ok(await svc.CreateDistrictAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string code, [FromBody] DistrictSaveDto dto)
        => await svc.UpdateDistrictAsync(code, dto) ? NoContent() : NotFound();
}

[Route("api/lookups")]
public class LookupsController(MastersService svc) : ApiControllerBase
{
    [HttpGet("ministries")] public async Task<IActionResult> Ministries() => Ok(await svc.GetMinistryLookupAsync());
    [HttpGet("departments")] public async Task<IActionResult> Departments() => Ok(await svc.GetDepartmentLookupAsync());
    [HttpGet("designations")] public async Task<IActionResult> Designations([FromQuery] int deptId) => Ok(await svc.GetDesignationLookupAsync(deptId));
    [HttpGet("designations/all")] public async Task<IActionResult> AllDesignations() => Ok(await svc.GetAllDesignationLookupAsync());
}
