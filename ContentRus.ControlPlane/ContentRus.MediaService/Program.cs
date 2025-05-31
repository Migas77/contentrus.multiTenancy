using Microsoft.AspNetCore.Mvc;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
Env.Load();

// var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
// var containerName = "media-container";


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// JWT 
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var key = Encoding.ASCII.GetBytes(jwtKey);


Console.WriteLine("Before AddJwtBearer");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    Console.WriteLine("Inside AddJwtBearer");
    Console.WriteLine($"🔐 JWT key loaded: {jwtKey}");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            Console.WriteLine($"AUTH HEADER RAW: {authHeader}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("✅ Token successfully validated.");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ Token authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});
Console.WriteLine("After AddJwtBearer");



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
