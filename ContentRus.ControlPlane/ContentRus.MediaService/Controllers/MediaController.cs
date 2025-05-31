using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Azure.Storage.Sas;
using Azure.Storage;


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

    
    [HttpPost("upload-sas")]
    public IActionResult GetUploadSas([FromBody] string fileName)
    {
        var tenantId = GetTenantIdFromClaims();
        var blobName = $"{tenantId}/{fileName}";

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Write | Azure.Storage.Sas.BlobSasPermissions.Create);

        var sasToken = sasBuilder.ToSasQueryParameters(
            new Azure.Storage.StorageSharedKeyCredential(
                blobServiceClient.AccountName,
                GetAccountKeyFromEnvironment()
            )).ToString();

        var sasUri = $"{blobClient.Uri}?{sasToken}";

        return Ok(new { UploadUrl = sasUri });
    }

    private string GetAccountKeyFromEnvironment()
    {
        // Extrai a key da connection string
        var match = Regex.Match(_connectionString, "AccountKey=([^;]+)");
        if (!match.Success) throw new Exception("AccountKey not found in connection string.");
        return match.Groups[1].Value;
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
    [HttpGet("download-url/{fileName}")]
    public IActionResult GetDownloadUrl(string fileName)
    {
        var tenantId = GetTenantIdFromClaims();
        var blobName = $"{tenantId}/{fileName}";

        var blobClient = new BlobClient(_connectionString, _containerName, blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b", // b = blob
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var accountName = new BlobServiceClient(_connectionString).AccountName;
        var accountKey = GetAccountKeyFromEnvironment();
        var storageCredential = new StorageSharedKeyCredential(accountName, accountKey);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        return Ok(new { Url = sasUri.ToString() });
    }

    private Guid GetTenantIdFromClaims()
    {   
        
        Console.WriteLine("User.Identity.IsAuthenticated: " + User.Identity.IsAuthenticated);
        Console.WriteLine("Authentication Type: " + User.Identity.AuthenticationType);
        Console.WriteLine("Claims Count: " + User.Claims.Count());
        Console.WriteLine("==== Claims ====");
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
        }

        var tenantIdClaim = User.Claims
            .FirstOrDefault(c => c.Type.Equals("TenantId", StringComparison.OrdinalIgnoreCase))?.Value;

        if (tenantIdClaim == null)
            throw new UnauthorizedAccessException("TenantId not found in token.");

        return Guid.Parse(tenantIdClaim);
    }


}
