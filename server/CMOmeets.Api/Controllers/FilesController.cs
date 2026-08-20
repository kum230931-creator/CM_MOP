using CMOmeets.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMOmeets.Api.Controllers;

[Route("api/files")]
public class FilesController(AtrFileService files) : ApiControllerBase
{
    [HttpPost("atr")]
    [Authorize(Roles = "admin,officer,cmo_officer,nodal")]
    [RequestSizeLimit(AtrFileService.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadAtr(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        if (file.Length > AtrFileService.MaxBytes)
            return BadRequest(new { message = "File exceeds the 10 MB limit." });
        if (!files.IsAllowed(file.FileName))
            return BadRequest(new { message = "Only JPG, PNG, PDF, DOC and DOCX files are allowed." });

        var stored = await files.SaveAsync(file);
        return Ok(new { fileName = stored, originalName = file.FileName });
    }

    [HttpGet("atr/{name}")]
    public IActionResult GetAtr(string name)
        => files.TryResolve(name, out var path)
            ? PhysicalFile(path, AtrFileService.ContentType(name))
            : NotFound();
}
