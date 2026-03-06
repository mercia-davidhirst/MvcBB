using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MvcBB.App.Interfaces;
using MvcBB.App.Services;
using MvcBB.App.Middleware;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
var clientId = builder.Configuration["Api:ClientId"];
var clientSecret = builder.Configuration["Api:ClientSecret"];

if (string.IsNullOrEmpty(apiBaseUrl))
    throw new InvalidOperationException("API base URL is not configured");
if (string.IsNullOrEmpty(jwtSecretKey))
    throw new InvalidOperationException("JWT secret key is not configured");
if (string.IsNullOrEmpty(clientId))
    throw new InvalidOperationException("API client ID is not configured");
if (string.IsNullOrEmpty(clientSecret))
    throw new InvalidOperationException("API client secret is not configured");

// Add services to the container.
builder.Services.AddControllersWithViews();

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

// Add BBCode services
builder.Services.AddSingleton<ICoreBBCodeService, CoreBBCodeService>();
builder.Services.AddSingleton<IMvcBBCodeService, BBCodeService>();
builder.Services.AddSingleton<IBBCodeService>(sp => sp.GetRequiredService<IMvcBBCodeService>());
builder.Services.AddSingleton<IBBCodeManagementService>(sp => sp.GetRequiredService<IMvcBBCodeService>());

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

// Add authentication
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Allow token to be passed in cookie for SignalR
                var accessToken = context.Request.Cookies["X-Access-Token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
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
