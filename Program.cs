using Microsoft.AspNetCore.Identity;
using WebApplication1.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AuthDbContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AuthDbContext>();

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
    options.Password.RequireDigit = false;       // No number required
    options.Password.RequiredLength = 12;         // Minimum length of 8
    options.Password.RequireNonAlphanumeric = false; // No special character required
    options.Password.RequireUppercase = false;   // No uppercase letter required
    options.Password.RequireLowercase = false;   // No lowercase letter required
});

builder.Services.AddDataProtection();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
