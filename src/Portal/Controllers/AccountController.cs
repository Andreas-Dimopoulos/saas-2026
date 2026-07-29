using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.Controllers;

public class AccountController(UserManager<PortalUser> userManager, SignInManager<PortalUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new PortalUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        return LocalRedirectOrHome(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public IActionResult Profile()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return View(nameof(Login), new LoginViewModel());
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ModelState.AddModelError(string.Empty, "Error loading external login information.");
            return View(nameof(Login), new LoginViewModel());
        }

        // Already linked to a PortalUser from a previous sign-in - just sign them in.
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Your Google account did not share an email address, so we can't sign you in.");
            return View(nameof(Login), new LoginViewModel());
        }

        // See CLAUDE.md / commit history for the reasoning: this app never verifies local
        // registration emails, so auto-linking on email match would let an attacker who
        // pre-registers a victim's email absorb the victim's later, genuinely-verified Google
        // sign-in into an account the attacker already controls. Reject instead of linking.
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(string.Empty, "An account already exists for this email. Sign in with your password instead.");
            return View(nameof(Login), new LoginViewModel());
        }

        var emailVerified = bool.TryParse(info.Principal.FindFirstValue("email_verified"), out var verified) && verified;
        if (!emailVerified)
        {
            ModelState.AddModelError(string.Empty, "Google has not verified this email address, so we can't create an account with it.");
            return View(nameof(Login), new LoginViewModel());
        }

        var newUser = new PortalUser
        {
            UserName = email,
            Email = email,
            DisplayName = ResolveDisplayName(info.Principal, email)
        };

        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Login), new LoginViewModel());
        }

        var addLoginResult = await userManager.AddLoginAsync(newUser, info);
        if (!addLoginResult.Succeeded)
        {
            foreach (var error in addLoginResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Login), new LoginViewModel());
        }

        await signInManager.SignInAsync(newUser, isPersistent: false);
        return LocalRedirectOrHome(returnUrl);
    }

    private static string ResolveDisplayName(ClaimsPrincipal externalPrincipal, string email)
    {
        var name = externalPrincipal.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var givenName = externalPrincipal.FindFirstValue(ClaimTypes.GivenName);
        var surname = externalPrincipal.FindFirstValue(ClaimTypes.Surname);
        var fullName = string.Join(' ', new[] { givenName, surname }.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return email[..email.IndexOf('@')];
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
