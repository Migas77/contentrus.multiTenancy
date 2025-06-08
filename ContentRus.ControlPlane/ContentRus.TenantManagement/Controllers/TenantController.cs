using Microsoft.AspNetCore.Mvc;
using ContentRus.TenantManagement.Models;
using ContentRus.TenantManagement.Services;
using Microsoft.AspNetCore.Authorization;

namespace ContentRus.TenantManagement.Controllers;

/// <summary>
/// API endpoints for managing tenant configuration and details.
/// </summary>
[ApiController]
[Route("api/tenant")]
public class TenantController : ControllerBase
{
    private readonly TenantService _tenantService;

    public TenantController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Updates the state of the current tenant (e.g., Active, Cancelled, DeploymentSuccess).
    /// </summary>
    /// <param name="newState">New state to apply to the tenant.</param>
    /// <returns>No content if updated successfully, NotFound otherwise.</returns>
    [HttpPut("state")]
    public IActionResult UpdateTenantState([FromBody] TenantState newState)
    {
        var id = GetTenantIdFromClaims();
        var updated = _tenantService.UpdateTenantState(id, newState);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates the subscription tier of the current tenant.
    /// </summary>
    /// <param name="newTier">New subscription tier to assign.</param>
    /// <returns>No content if updated successfully, NotFound otherwise.</returns>
    [HttpPut("tier")]
    public IActionResult UpdateTenantTier([FromBody] TenantTier newTier)
    {
        var id = GetTenantIdFromClaims();
        var updated = _tenantService.UpdateTenantTier(id, newTier);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates general information (e.g., company name, address) of the current tenant.
    /// </summary>
    /// <param name="tenantInfo">DTO containing updated tenant info.</param>
    /// <returns>No content if updated successfully, NotFound otherwise.</returns>
    [HttpPut("info")]
    public IActionResult UpdateTenantInfo([FromBody] TenantInfoDTO tenantInfo)
    {
        var id = GetTenantIdFromClaims();

        var updated = _tenantService.UpdateTenantInfo(id, tenantInfo);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// Gets the current tenant's details based on authenticated user's token.
    /// </summary>
    /// <returns>Tenant details if found, otherwise NotFound.</returns>
    [Authorize]
    [HttpGet("")]
    public IActionResult GetTenant()
    {

        var tenantId = GetTenantIdFromClaims();
        var tenant = _tenantService.GetTenant(tenantId);

        return tenant is not null ? Ok(tenant) : NotFound();
    }

    /// <summary>
    /// Lists all available tenant subscription tiers.
    /// </summary>
    /// <returns>List of tenant tiers.</returns>
    [HttpGet("tiers")]
    public IActionResult GetAllTenantTiers()
    {
        var tenantTiers = _tenantService.GetAllTenantTiers();
        return Ok(tenantTiers);
    }

    private Guid GetTenantIdFromClaims()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (tenantIdClaim == null)
            throw new UnauthorizedAccessException("TenantId not found in token.");
        Console.WriteLine($"TenantId from claims: {tenantIdClaim}");

        return Guid.Parse(tenantIdClaim);
    }
}
