using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentRus.Onboarding.Services;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly RabbitMQTenantStatusPublisher _tenantStatusPublisher;

    /// <summary>
    /// Default constructor for Media Controller.
    /// </summary>
    /// <param name="configuration">The configuration settings for the application.</param>
    /// <param name="tenantStatusPublisher">The RabbitMQ publisher for tenant status updates.</param>
    public WebhookController(IConfiguration configuration, RabbitMQTenantStatusPublisher tenantStatusPublisher)
    {
        _configuration = configuration;
        _tenantStatusPublisher = tenantStatusPublisher;
    }

    /// <summary>
    /// Webhook to handle deployment status updates from ArgoCD, which communicates through RabbitMQ to the appropriate services.
    /// </summary> 
    /// <param name="deploymentMessage">The deployment message containing status updates.</param>
    [HttpPost("deployment-status")]
    public async Task<IActionResult> setDeploymentStatus([FromBody] DeploymentMessage deploymentMessage)
    {
        if (deploymentMessage == null)
            return BadRequest("Invalid deployment message");

        var status = deploymentMessage.health.ToLower() == "healthy" 
            ? "success" 
            : "failed";

        var deploymentStatus = new DeploymentStatusEvent
        {
            Type = "deployment",
            Status = status,
            TenantID = deploymentMessage.tenantNamespace,
        };

        await _tenantStatusPublisher.PublishAsync(deploymentStatus);

        return Ok(new { message = "Deployment status comunicated successfully." });
    }

    /// <summary>
    /// Deployment status updates from ArgoCD.
    /// </summary>
    public class DeploymentMessage
    {
        /// <summary>
        /// The name of the argoCD application.
        /// </summary>
        required public string app { get; set; }

        /// <summary>
        /// The sync status of the deployment.
        /// </summary>
        required public string status { get; set; }

        /// <summary>
        /// The health status of the deployment.
        /// </summary>
        required public string health { get; set; }

        /// <summary>
        /// The namespace of the tenant where the deployment is happening.
        /// </summary>
        required public string tenantNamespace { get; set; }

        /// <summary>
        /// The timestamp of the deployment status update.
        /// </summary>
        required public string timestamp { get; set; }

        public override string ToString()
        {
            return $"DeploymentMessage [app={app}, status={status}, health={health}, tenantNamespace={tenantNamespace}, timestamp={timestamp}]";
        }
    }
    
}