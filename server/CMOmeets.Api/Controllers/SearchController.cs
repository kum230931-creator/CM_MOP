using CMOmeets.Application.Search;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/search")]
public class SearchController(SearchService svc) : ApiControllerBase
{
    // Global ("deep") search across meetings, action points, officers and departments — scope-aware.
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q)
        => Ok(await svc.SearchAsync(q, Scope()));
}
