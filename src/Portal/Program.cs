using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Authentication;
using Portal.Data;
using Portal.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PortalContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PortalContext")));
builder.Services.AddIdentity<PortalUser, IdentityRole>()
    .AddEntityFrameworkStores<PortalContext>();

// Parameterless AddAuthentication() registers an additional scheme without touching
// AuthenticationOptions.DefaultScheme/DefaultSignInScheme/DefaultChallengeScheme, which
// AddIdentity() above already set to the cookie scheme. Calling AddAuthentication("Basic")
// instead would silently overwrite that default for the whole site.
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationDefaults.AuthenticationScheme, options => { });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }
