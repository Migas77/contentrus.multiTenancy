using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Security.Claims;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly string _storageAccountName = "contentrustenants";
    private readonly IConfiguration _configuration;

    public MediaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            var tenantId = GetTenantIdFromClaims();
            var blobServiceClient = await GetBlobServiceClientAsync(tenantId);
            var containerName = $"tenant{tenantId}";
            
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Generate a unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return Ok(new { 
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
            var tenantId = GetTenantIdFromClaims();
            var blobServiceClient = await GetBlobServiceClientAsync(tenantId);
            var containerName = $"tenant{tenantId}";
            
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
            var tenantId = GetTenantIdFromClaims();
            var blobServiceClient = await GetBlobServiceClientAsync(tenantId);
            var containerName = $"tenant{tenantId}";
            
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
            var tenantId = GetTenantIdFromClaims();
            var blobServiceClient = await GetBlobServiceClientAsync(tenantId);
            var containerName = $"tenant{tenantId}";
            
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

    [HttpGet("sas-info")]
    public async Task<IActionResult> GetSasInfo()
    {
        try
        {
            var tenantId = GetTenantIdFromClaims();
            var sasToken = await GetSasTokenFromConfigurationAsync(tenantId);
            var expiryDate = await GetSasExpiryFromConfigurationAsync(tenantId);
            
            return Ok(new
            {
                HasValidSasToken = !string.IsNullOrEmpty(sasToken),
                ExpiryDate = expiryDate,
                ContainerName = $"tenant{tenantId}"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"SAS info retrieval failed: {ex.Message}");
        }
    }

    private async Task<BlobServiceClient> GetBlobServiceClientAsync(Guid tenantId)
    {
        var sasToken = await GetSasTokenFromConfigurationAsync(tenantId);
        
        if (string.IsNullOrEmpty(sasToken))
        {
            throw new UnauthorizedAccessException("SAS token not found for tenant.");
        }

        var blobServiceUri = new Uri($"https://{_storageAccountName}.blob.core.windows.net?{sasToken}");
        return new BlobServiceClient(blobServiceUri);
    }

    private async Task<string> GetSasTokenFromConfigurationAsync(Guid tenantId)
    {
        // Option 1: From Kubernetes secret via environment variables
        var sasToken = Environment.GetEnvironmentVariable($"AZUREBLOB_SAS_TOKEN_T{tenantId}");
        
        if (!string.IsNullOrEmpty(sasToken))
            return sasToken;

        // Option 2: From configuration (appsettings.json or other config providers)
        sasToken = _configuration[$"AzureBlob:Tenants:t{tenantId}:SasToken"];
        
        if (!string.IsNullOrEmpty(sasToken))
            return sasToken;

        // Option 3: Generic fallback from configuration
        sasToken = _configuration["AzureBlob:SasToken"];
        
        return sasToken ?? string.Empty;
    }

    private async Task<string> GetSasExpiryFromConfigurationAsync(Guid tenantId)
    {
        // Option 1: From Kubernetes secret via environment variables
        var expiryDate = Environment.GetEnvironmentVariable($"AZUREBLOB_EXPIRY_DATE_T{tenantId}");
        
        if (!string.IsNullOrEmpty(expiryDate))
            return expiryDate;

        // Option 2: From configuration
        expiryDate = _configuration[$"AzureBlob:Tenants:t{tenantId}:ExpiryDate"];
        
        if (!string.IsNullOrEmpty(expiryDate))
            return expiryDate;

        return string.Empty;
    }

    private Guid GetTenantIdFromClaims()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (tenantIdClaim == null)
            throw new UnauthorizedAccessException("TenantId not found in token.");

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            throw new UnauthorizedAccessException("Invalid TenantId format in token.");

        return tenantId;
    }
}
