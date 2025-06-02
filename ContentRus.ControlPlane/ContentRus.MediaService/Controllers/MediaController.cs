using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Security.Claims;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly string _connectionString;
    private readonly string _containerName = "media-container";

    public MediaController()
    {
        _connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Missing AZURE_STORAGE_CONNECTION_STRING");
    }


    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var tenantId = GetTenantIdFromClaims();

        var blobClient = new BlobContainerClient(_connectionString, _containerName);
        await blobClient.CreateIfNotExistsAsync();

        var fileName = $"{tenantId}/{file.FileName}";
        var blob = blobClient.GetBlobClient(fileName);

        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, overwrite: true);

        return Ok(new { Url = blob.Uri.ToString() });
    }

    [Authorize]
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> Download(string fileName)
    {
        var tenantId = GetTenantIdFromClaims();

        var blobClient = new BlobContainerClient(_connectionString, _containerName);
        var blob = blobClient.GetBlobClient($"{tenantId}/{fileName}");

        if (await blob.ExistsAsync())
        {
            var stream = await blob.OpenReadAsync();
            return File(stream, "application/octet-stream", fileName);
        }

        return NotFound();
    }

    [Authorize]
    [HttpDelete("delete/{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        var tenantId = GetTenantIdFromClaims();

        var blobClient = new BlobContainerClient(_connectionString, _containerName);
        var blob = blobClient.GetBlobClient($"{tenantId}/{fileName}");

        var deleted = await blob.DeleteIfExistsAsync();

        if (deleted)
            return Ok(new { Message = $"File '{fileName}' deleted successfully." });

        return NotFound(new { Message = $"File '{fileName}' not found." });
    }


    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> ListFiles()
    {
        var tenantId = GetTenantIdFromClaims();

        var blobClient = new BlobContainerClient(_connectionString, _containerName);
        var result = new List<string>();

        await foreach (var blobItem in blobClient.GetBlobsAsync(prefix: $"{tenantId}/"))
        {
            var fileName = blobItem.Name.Substring($"{tenantId}/".Length);
            result.Add(fileName);
        }

        return Ok(result);
    }


    private Guid GetTenantIdFromClaims()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (tenantIdClaim == null)
            throw new UnauthorizedAccessException("TenantId not found in token.");

        return Guid.Parse(tenantIdClaim);
    }

}
