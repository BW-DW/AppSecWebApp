using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using BookWorms.Model;
using BookWorms.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AuthDbContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();  // to enable token-based email confirmation

builder.Services.AddScoped<AuditLoggerService>();
builder.Services.AddHttpContextAccessor();  // grab user ip
builder.Services.AddHttpClient();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddScoped<EmailSender>();
// Register EmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options
=>
{
    options.Cookie.Name = "MyCookieAuth";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBelongToHRDepartment",
    policy => policy.RequireClaim("Department", "HR"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure cookies work over HTTPS
    options.Cookie.SameSite = SameSiteMode.None; // Prevent cookie rejection on different schemes
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;       
    options.Password.RequiredLength = 12;         
    options.Password.RequireNonAlphanumeric = true; 
    options.Password.RequireUppercase = true;   
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Lockout duration
    options.Lockout.MaxFailedAccessAttempts = 3; // Max attempts before lockout
    options.Lockout.AllowedForNewUsers = true; // Enable lockout for all users
});

builder.Services.AddDataProtection();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddDistributedMemoryCache(); //save session in memory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/errors/{0}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
