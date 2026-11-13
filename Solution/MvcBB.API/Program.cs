using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MvcBB.Shared.Services;
using Microsoft.AspNetCore.Diagnostics;
using MvcBB.Shared.Interfaces;
using MvcBB.API.InMemory;
using MvcBB.API.SQL;
using MvcBB.API.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register data layer. DataProvider selects which backing store the API runs
// against; defaults to "InMemory" (no configuration/external dependency
// required) if unset. "Sql" needs ConnectionStrings:SqlServer and the schema
// in Database/SQL deployed; "PostgreSql" needs ConnectionStrings:PostgreSql
// and the schema in Database/PostGresSql deployed.
var dataProvider = builder.Configuration["DataProvider"] ?? "InMemory";
switch (dataProvider)
{
    case "Sql":
        builder.Services.AddSqlRepositories(builder.Configuration);
        break;
    case "PostgreSql":
        builder.Services.AddPostgreSqlRepositories(builder.Configuration);
        break;
    case "InMemory":
        builder.Services.AddInMemoryRepositories();
        break;
    default:
        throw new InvalidOperationException($"Unknown DataProvider '{dataProvider}'. Expected 'InMemory', 'Sql' or 'PostgreSql'.");
}

// Register services
builder.Services.AddSingleton<ICoreBBCodeService, CoreBBCodeService>();
builder.Services.AddScoped<IBBCodeManagementService, MvcBB.API.Services.BBCodeManagementService>();
builder.Services.AddScoped<IAnnouncementManagementService, MvcBB.API.Services.AnnouncementManagementService>();
// Real SMTP delivery kicks in only once Smtp:Host is actually set - a fresh
// install with nothing configured keeps logging the email instead (see
// LoggingEmailSender) rather than failing outright.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
{
    builder.Services.AddSingleton<MvcBB.API.Services.IEmailSender, MvcBB.API.Services.SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<MvcBB.API.Services.IEmailSender, MvcBB.API.Services.LoggingEmailSender>();
}
builder.Services.AddSingleton<MvcBB.API.Services.IDisplaySettingsService, MvcBB.API.Services.DisplaySettingsService>();
builder.Services.AddSingleton<MvcBB.API.Services.IPasswordPolicyService, MvcBB.API.Services.PasswordPolicyService>();

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT secret key is not configured");

if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey.Length < 32)
{
    throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS.
// This is a distinct concern from the "AllowedHosts" setting below: AllowedHosts
// controls ASP.NET Core's built-in Host-header filtering (a bare pattern like
// "*" or "localhost"), while CORS needs full origin URLs (scheme + host + port)
// for the MVC app(s) allowed to call this API from the browser.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsAllowedOrigins == null || corsAllowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins is not configured");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Add global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            await context.Response.WriteAsJsonAsync(new 
            { 
                StatusCode = 500,
                Message = "An internal server error occurred."
            });
        }
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowMvcApp");

app.UseAuthentication();
app.UseAuthorization();

// Default page: a bare visual status check (no auth, no other details) for
// whoever is hosting this to confirm at a glance that the deployed API can
// actually reach its configured database - see IDatabaseHealthCheck.
app.MapGet("/", (IDatabaseHealthCheck healthCheck) =>
{
    bool healthy;
    try
    {
        healthy = healthCheck.IsHealthy();
    }
    catch
    {
        healthy = false;
    }

    var color = healthy ? "#2ecc71" : "#e74c3c";
    var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <title>MvcBB API Status</title>
            <style>
                body {
                    margin: 0;
                    height: 100vh;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    background: #111;
                    font-family: -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                }
                .status {
                    font-size: 2.5rem;
                    color: #eee;
                    display: flex;
                    align-items: center;
                    gap: 0.6em;
                }
                .dot {
                    width: 0.9em;
                    height: 0.9em;
                    border-radius: 50%;
                    background: {{color}};
                }
            </style>
        </head>
        <body>
            <div class="status">Status is <span class="dot"></span></div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html", System.Text.Encoding.UTF8,
        healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();

app.Run(); 