using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Security.Claims;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _tenantId;
    private readonly string _storageAccountName;
    private readonly string _sasToken;

    public MediaController(IConfiguration configuration)
    {
        _configuration = configuration;
        _tenantId = _configuration["tenantId"];
        _storageAccountName = _configuration["storageAccountName"];
        _sasToken = _configuration["sasToken"];
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            var blobServiceClient = await GetBlobServiceClientAsync();
            var containerName = $"tenant{_tenantId}";

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Generate a unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return Ok(new
            {
                FileName = fileName,
                OriginalName = file.FileName,
                Url = blobClient.Uri.ToString(),
                Size = file.Length
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Upload failed: {ex.Message}");
        }
    }

    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> Download(string fileName)
    {
        try
        {
            var blobServiceClient = await GetBlobServiceClientAsync();
            var containerName = $"tenant{_tenantId}";

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                var blobDownloadInfo = await blobClient.DownloadAsync();
                var contentType = blobDownloadInfo.Value.Details.ContentType ?? "application/octet-stream";

                return File(blobDownloadInfo.Value.Content, contentType, fileName);
            }

            return NotFound($"File '{fileName}' not found.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Download failed: {ex.Message}");
        }
    }

    [HttpDelete("delete/{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        try
        {
            var blobServiceClient = await GetBlobServiceClientAsync();
            var containerName = $"tenant{_tenantId}";

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
                return Ok(new { Message = $"File '{fileName}' deleted successfully." });

            return NotFound(new { Message = $"File '{fileName}' not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Delete failed: {ex.Message}");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListFiles()
    {
        try
        {
            var blobServiceClient = await GetBlobServiceClientAsync();
            var containerName = $"tenant{_tenantId}";

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var files = new List<object>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                files.Add(new
                {
                    Name = blobItem.Name,
                    Size = blobItem.Properties.ContentLength,
                    LastModified = blobItem.Properties.LastModified,
                    ContentType = blobItem.Properties.ContentType
                });
            }

            return Ok(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"List failed: {ex.Message}");
        }
    }

    private async Task<BlobServiceClient> GetBlobServiceClientAsync()
    {
        if (string.IsNullOrEmpty(_sasToken))
        {
            throw new UnauthorizedAccessException("SAS token not found for tenant.");
        }

        var queryParams = System.Web.HttpUtility.ParseQueryString(_sasToken);
        var expiryString = queryParams["se"];
        if (expiryString == null)
        {
            throw new UnauthorizedAccessException("SAS token expiry (se) not found.");
        }

        if (!DateTimeOffset.TryParse(expiryString, out var expiry))
        {
            throw new UnauthorizedAccessException("Invalid SAS token expiry.");
        }

        if (expiry <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("SAS token has expired.");
        }

        var blobServiceUri = new Uri($"https://{_storageAccountName}.blob.core.windows.net?{_sasToken}");
        return new BlobServiceClient(blobServiceUri);
    }

    
}
