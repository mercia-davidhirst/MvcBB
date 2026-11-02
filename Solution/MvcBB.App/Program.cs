using Microsoft.AspNetCore.Authentication.Cookies;
using MvcBB.App.Interfaces;
using MvcBB.App.Services;
using MvcBB.App.Middleware;
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
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseCors("DefaultPolicy");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add custom middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
