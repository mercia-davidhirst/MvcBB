using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MvcBB.Shared.Services;
using Microsoft.AspNetCore.Diagnostics;
using MvcBB.Shared.Interfaces;
using MvcBB.API.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register data layer (swap to MvcBB.API.SQL and AddSqlRepositories() for SQL Server,
// or MvcBB.API.PostgreSql and AddPostgreSqlRepositories() for PostgreSQL)
builder.Services.AddInMemoryRepositories();

// Register services
builder.Services.AddSingleton<ICoreBBCodeService, CoreBBCodeService>();
builder.Services.AddScoped<IBBCodeManagementService, MvcBB.API.Services.BBCodeManagementService>();
builder.Services.AddSingleton<MvcBB.API.Services.IDisplaySettingsService, MvcBB.API.Services.DisplaySettingsService>();

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

app.MapControllers();

app.Run(); 