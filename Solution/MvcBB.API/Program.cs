using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Text;
using MvcBB.Shared.Services;
using Microsoft.AspNetCore.Diagnostics;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Mod;
using MvcBB.API.Filters;
using MvcBB.API.InMemory;
using MvcBB.API.Mods;
using MvcBB.API.SQL;
using MvcBB.API.PostgreSql;
using MvcBB.ModKit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvcBuilder = builder.Services.AddControllers(options =>
{
    // Soft-disable enforcement for mod controllers - see ModEnabledFilter.
    // No-op for every core controller (none carry [Mod]).
    options.Filters.Add<ModEnabledFilter>();
    // The App's shared service token can read but never change anything -
    // see ServiceTokenReadOnlyFilter.
    options.Filters.Add<ServiceTokenReadOnlyFilter>();
});
builder.Services.AddMemoryCache();

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

// Mod framework: discover drop-in mod assemblies from a "Mods" folder next
// to this process's own build output. A mod ships as a plain DLL built
// against MvcBB.ModKit - dropping it in here (and in MvcBB.App's own Mods
// folder for its App-side counterpart) is the entire "installation" step;
// nothing above this line ever needs to change for a new mod. Mods live in
// their own separate solutions/projects, never in this one - see
// MvcBB.ModKit.IApiMod for the contract a mod's API-side assembly implements.
var modsFolder = Path.Combine(AppContext.BaseDirectory, "Mods");
var modAssemblies = ModLoader.LoadModAssemblies(modsFolder);
var apiMods = ModLoader.CreateMods<IApiMod>(modAssemblies);
foreach (var mod in apiMods)
{
    mod.ConfigureServices(builder.Services, builder.Configuration);
}
foreach (var modAssembly in modAssemblies)
{
    mvcBuilder.ConfigureApplicationPartManager(apm => apm.ApplicationParts.Add(new AssemblyPart(modAssembly)));
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
// Admin emails to members: queued by AdminEmailsController, sent in throttled
// batches in the background by AdminEmailDispatcher (see AdminEmailOptions
// for the AdminEmail:* settings).
builder.Services.Configure<MvcBB.API.Services.AdminEmailOptions>(builder.Configuration.GetSection(MvcBB.API.Services.AdminEmailOptions.SectionName));
builder.Services.AddSingleton<MvcBB.API.Services.AdminEmailQueue>();
builder.Services.AddHostedService<MvcBB.API.Services.AdminEmailDispatcher>();
// Scoped, not Singleton: both now read/write through a repository that's
// itself Scoped for the Sql/PostgreSql providers (a new connection per
// request) - a Singleton can't depend on a Scoped service in ASP.NET Core's
// DI container.
builder.Services.AddScoped<MvcBB.API.Services.IDisplaySettingsService, MvcBB.API.Services.DisplaySettingsService>();
builder.Services.AddScoped<MvcBB.API.Services.IPasswordPolicyService, MvcBB.API.Services.PasswordPolicyService>();
// One shared rule for who can see which boards, threads and posts - see
// ContentVisibilityService.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MvcBB.API.Services.IContentVisibilityService, MvcBB.API.Services.ContentVisibilityService>();

// Configure JWT Authentication - issuing and validating both go through
// JwtTokenService (issuer/audience checks, UTF-8 key, separate user and
// service signing keys).
_ = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT secret key is not configured");
var jwtTokenService = new MvcBB.API.Services.JwtTokenService(builder.Configuration);
builder.Services.AddSingleton(jwtTokenService);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = jwtTokenService.CreateValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Right kind of token for its key and audience, and - for a
                // user token - the session hasn't been ended by a password
                // change, demotion or lock since it was issued.
                var error = jwtTokenService.CheckTokenKind(context) ?? jwtTokenService.CheckUserSession(context);
                if (error != null)
                {
                    context.Fail(error);
                }
                return Task.CompletedTask;
            }
        };
    });

// Secure by default: any endpoint without its own [Authorize]/[AllowAnonymous]
// requires an authenticated caller (a signed-in user's JWT or the App's
// ApiClient token). Endpoints that must stay public for guests - login,
// registration, password reset, the service token, and the read-only
// browsing endpoints - are marked [AllowAnonymous] explicitly.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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

// Mod framework: register every discovered mod in the Mods table and run
// InstallAsync for any whose declared Version hasn't been installed yet.
// Done once, after Build(), inside its own scope so it can resolve the
// now-fully-configured container (connection factories, and anything the
// mod's own ConfigureServices registered above).
using (var modStartupScope = app.Services.CreateScope())
{
    var modRepository = modStartupScope.ServiceProvider.GetRequiredService<IModRepository>();
    var coreVersion = typeof(IApiMod).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);

    foreach (var mod in apiMods)
    {
        var isCompatible = ModLoader.IsCompatible(mod.MinCoreVersion, mod.MaxCoreVersion, coreVersion);
        var status = isCompatible ? ModStatus.Compatible : ModStatus.Incompatible;
        modRepository.Upsert(mod.Id, mod.Name, mod.Version, mod.MinCoreVersion, mod.MaxCoreVersion, status);

        var record = modRepository.GetById(mod.Id);
        if (!isCompatible || record == null || record.InstalledVersion == mod.Version)
            continue;

        IDbConnection? connection = dataProvider switch
        {
            "Sql" => modStartupScope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>().CreateConnection(),
            "PostgreSql" => modStartupScope.ServiceProvider.GetRequiredService<IPostgreSqlConnectionFactory>().CreateConnection(),
            _ => null
        };
        using (connection)
        {
            await mod.InstallAsync(new ModInstallContext(dataProvider, connection));
        }
        modRepository.SetInstalledVersion(mod.Id, mod.Version);
    }
}

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

// The API only returns JSON (plus the bare status page), so it never needs
// to be framed, and responses must never be content-sniffed into HTML.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
        return Task.CompletedTask;
    });
    await next();
});

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
}).AllowAnonymous();

app.MapControllers();

app.Run(); 