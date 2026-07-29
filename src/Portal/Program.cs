using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Authentication;
using Portal.Data;
using Portal.Hubs;
using Portal.Models;
using Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PortalContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PortalContext")));
builder.Services.AddIdentity<PortalUser, IdentityRole>()
    .AddEntityFrameworkStores<PortalContext>();
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationService>();

// Parameterless AddAuthentication() registers an additional scheme without touching
// AuthenticationOptions.DefaultScheme/DefaultSignInScheme/DefaultChallengeScheme, which
// AddIdentity() above already set to the cookie scheme. Calling AddAuthentication("Basic")
// instead would silently overwrite that default for the whole site.
var authenticationBuilder = builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationDefaults.AuthenticationScheme, options => { });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// Remote authentication handlers (anything with a CallbackPath, like Google's /signin-google)
// get initialized by the authentication middleware on *every* request, not just requests that
// touch them, so they can check "is this my OAuth callback?" before the rest of the pipeline
// runs. GoogleOptions.Validate() unconditionally throws if ClientId is empty, so registering
// AddGoogle with unset secrets doesn't just break the Google button - it 500s the entire site,
// including for anyone who hasn't configured Google credentials yet. Only register the scheme
// once both secrets actually exist.
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        // AddIdentity() above already made this the default sign-in scheme, but the point of
        // this line is that the student shouldn't have to know that to explain it.
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClaimActions.MapJsonKey("email_verified", "email_verified", ClaimValueTypes.Boolean);

        // Without this, Google returning an error (e.g. the user cancels consent) throws an
        // unhandled exception instead of a page. Redirect back into our own callback action,
        // which already has a branch for remoteError, instead of a 500.
        options.Events = new OAuthEvents
        {
            OnRemoteFailure = context =>
            {
                var message = Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error.");
                context.Response.Redirect($"/Account/ExternalLoginCallback?remoteError={message}");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
}

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

app.MapHub<ConversationHub>("/hubs/conversations");
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }
