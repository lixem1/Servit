using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Servit.Api.Extensions;
using Servit.Api.Hubs;
using Servit.Api.Services;
using Servit.Domain.Entities;
using Servit.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ServitDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseNetTopologySuite()));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ServitDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, HubUserIdProvider>();
builder.Services.AddMemoryCache();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured. Run 'dotnet user-secrets set Jwt:Key <value>'.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS: Restrict to mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("content-disposition");
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServitDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Security: Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";
    await next();
});

app.UseHttpsRedirection();

app.UseCors("AllowMobileApp");

app.UseAuthentication();

// Invalidate tokens for deleted accounts
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.GetUserId();
        var db = context.RequestServices.GetRequiredService<ServitDbContext>();
        var deletedAt = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DeletedAt)
            .FirstOrDefaultAsync();
        if (deletedAt is not null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }
    await next();
});

// Rate limiting: Simple in-memory implementation
var requestCounts = new Dictionary<string, (int count, DateTime resetTime)>();
app.Use(async (context, next) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var endpoint = context.Request.Path.ToString().ToLower();
    var key = $"{clientIp}:{endpoint}";
    var now = DateTime.UtcNow;

    if (!requestCounts.ContainsKey(key))
    {
        requestCounts[key] = (1, now.AddSeconds(60));
    }
    else
    {
        var (count, resetTime) = requestCounts[key];
        if (now > resetTime)
        {
            requestCounts[key] = (1, now.AddSeconds(60));
        }
        else
        {
            requestCounts[key] = (count + 1, resetTime);
        }
    }

    var (currentCount, _) = requestCounts[key];
    var limit = endpoint.Contains("/auth/login") ? 10 :
                endpoint.Contains("/auth/register") ? 5 :
                endpoint.Contains("/auth/forgot-password") ? 3 : 100;

    if (currentCount > limit)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = "60";
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapHub<ServiceRequestsHub>("/hubs/service-requests");

app.Run();
