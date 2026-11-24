using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using MvcBB.App.Extensions;
using MvcBB.App.Filters;
using MvcBB.App.Interfaces;
using MvcBB.App.Services;
using MvcBB.App.Middleware;
using MvcBB.App.Mods;
using MvcBB.ModKit;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
var clientId = builder.Configuration["Api:ClientId"];
var clientSecret = builder.Configuration["Api:ClientSecret"];

if (string.IsNullOrEmpty(apiBaseUrl))
    throw new InvalidOperationException("API base URL is not configured");
if (string.IsNullOrEmpty(clientId))
    throw new InvalidOperationException("API client ID is not configured");
if (string.IsNullOrEmpty(clientSecret))
    throw new InvalidOperationException("API client secret is not configured");

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    // Soft-disable enforcement for mod controllers - see Filters/ModEnabledFilter.cs.
    // No-op for every core controller (none carry [Mod]).
    options.Filters.Add<ModEnabledFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Mod framework: discover drop-in mod assemblies from a "Mods" folder next
// to this process's own build output - the App-side counterpart of
// MvcBB.API's own mod loader. A mod's controllers *and* precompiled Razor
// views (it's built as a Razor Class Library) both come from the same
// assembly via ApplicationParts. Mods live in their own separate solutions/
// projects, never in this one - see MvcBB.ModKit.IAppMod for the contract a
// mod's App-side assembly implements, built as a Razor Class Library
// (AddRazorSupportForMvc, with RazorCompileOnBuild/OnPublish forced to true
// since a drop-in DLL has no file watcher to fall back on for runtime
// compilation).
var modsFolder = Path.Combine(AppContext.BaseDirectory, "Mods");
var modAssemblies = ModLoader.LoadModAssemblies(modsFolder);
var appMods = ModLoader.CreateMods<IAppMod>(modAssemblies);
foreach (var mod in appMods)
{
    mod.ConfigureServices(builder.Services, builder.Configuration);
}
// A mod's assembly declares its own ApplicationPartFactory (Razor Class
// Libraries point this at one that yields both a controller-discovery part
// and a CompiledRazorAssemblyPart for its views) - going through that
// factory, rather than adding a bare AssemblyPart ourselves, is what's
// required for the mod's precompiled views to actually be found at render
// time; a bare AssemblyPart only ever surfaces its controllers.
var modApplicationParts = modAssemblies
    .SelectMany(a => ApplicationPartFactory.GetApplicationPartFactory(a).GetApplicationParts(a))
    .ToList();
mvcBuilder.ConfigureApplicationPartManager(apm =>
{
    foreach (var part in modApplicationParts)
        apm.ApplicationParts.Add(part);
});
// Nav rendering (see _Layout.cshtml) needs to know which mods are physically
// present, separately from which ones are currently enabled.
builder.Services.AddSingleton<IReadOnlyList<IAppMod>>(appMods);
// View slots: which mod view components render into which core view slot -
// see Mods/ModSlotRegistry.cs and docs/ModKit-ViewSlots.md. Built (and each
// contribution checked) once, straight after the app is built below.
builder.Services.AddSingleton(sp =>
    ModSlotRegistry.Build(appMods, sp.GetRequiredService<ILogger<ModSlotRegistry>>()));

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure HTTP client for API communication
builder.Services.AddHttpClient("MvcBBApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(apiBaseUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add BBCode services.
// Parse/strip/validate are stateless and stay local (CoreBBCodeService implements
// IBBCodeService directly, so both interfaces resolve to the same instance).
// Tag/smilie management now lives behind MvcBB.API's repository layer, so
// IMvcBBCodeService is an HTTP client instead of an in-memory store.
builder.Services.AddSingleton<CoreBBCodeService>();
builder.Services.AddSingleton<ICoreBBCodeService>(sp => sp.GetRequiredService<CoreBBCodeService>());
builder.Services.AddSingleton<IBBCodeService>(sp => sp.GetRequiredService<CoreBBCodeService>());
builder.Services.AddScoped<IMvcBBCodeService, BBCodeService>();

// Register API services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IThreadService, ThreadService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddHttpClient<ITurnstileService, TurnstileService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IRankService, RankService>();
builder.Services.AddScoped<IWordFilterService, WordFilterService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IModsService, ModsService>();
builder.Services.AddScoped<IAdminEmailService, AdminEmailService>();

// Add authentication - the App authenticates browser sessions with a cookie;
// the JWTs issued by MvcBB.API are only ever used for the App's own outbound
// HTTP calls to the API, never for validating inbound requests to the App.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        // Re-check the session with the API on every request, so locking,
        // demoting or a password change ends it straight away - see
        // CookieSessionValidator.
        options.Events.OnValidatePrincipal = CookieSessionValidator.ValidateAsync;
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => 
        policy.RequireRole("Administrator"));
        
    options.AddPolicy("RequireModerator", policy => 
        policy.RequireRole("Administrator", "Moderator"));
        
    options.AddPolicy("ManageBoards", policy => 
        policy.RequireRole("Administrator"));
        
    options.AddPolicy("ManageThreads", policy => 
        policy.RequireRole("Administrator", "Moderator"));
        
    options.AddPolicy("ManageUsers", policy => 
        policy.RequireRole("Administrator"));
        
    options.AddPolicy("CreateThread", policy =>
        policy.RequireAuthenticatedUser());
        
    options.AddPolicy("CreatePost", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Build the slot registry now, so a broken mod contribution is logged at
// startup rather than on the first page view.
app.Services.GetRequiredService<ModSlotRegistry>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Before static files, so uploaded images get nosniff and the CSP too.
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
// Static files a mod embeds in its own DLL, at /_mods/{modId}/... - see
// Extensions/ModStaticFilesExtensions.cs. Placed with the other static
// files so they get the same security headers and skip auth/routing.
app.UseModStaticFiles(appMods);
app.UseSession();
app.UseCors("DefaultPolicy");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add custom middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RequirePasswordChangeMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
