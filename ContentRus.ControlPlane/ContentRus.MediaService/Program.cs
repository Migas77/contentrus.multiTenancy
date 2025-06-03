using Microsoft.AspNetCore.Mvc;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
Env.Load();

var requiredEnvVars = new Dictionary<string, string>
{
    { "storageAccountName", "STORAGE_ACCOUNT_NAME" },
    { "tenantId", "TENANT_ID" },
    { "sasToken", "SAS_TOKEN" }
};

// Validate and add to configuration
foreach (var (configKey, envVar) in requiredEnvVars)
{
    var value = Environment.GetEnvironmentVariable(envVar);
    if (string.IsNullOrEmpty(value))
    {
        throw new InvalidOperationException($"Required environment variable {envVar} is not set");
    }
    builder.Configuration[configKey] = value;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowInterfaceRequests",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors("AllowInterfaceRequests");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
