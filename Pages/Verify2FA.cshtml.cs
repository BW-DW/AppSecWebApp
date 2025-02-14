using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using BookWorms.Model;
using BookWorms.Services;

public class Verify2FAModel : PageModel
{
    [BindProperty]
    public string TwoFactorCode { get; set; }

    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly AuditLoggerService auditLogger;

    public Verify2FAModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuditLoggerService auditLogger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.auditLogger = auditLogger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("2FAUserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("Login");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToPage("Login");
        }

        // verify 2fa token
        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, TwoFactorCode);

        if (!isValid)
        {
            ModelState.AddModelError("", "Invalid 2FA code.");
            return Page();
        }

        // Generate a new session ID after successful 2FA verification
        string newSessionId = Guid.NewGuid().ToString();
        user.SessionId = newSessionId;
        await userManager.UpdateAsync(user);

        // Store user details in session after 2FA verification
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.UserName);
        HttpContext.Session.SetString("SessionId", user.SessionId);

        // Reset failed login attempts after successful authentication
        await userManager.ResetAccessFailedCountAsync(user);

        // Log successful 2FA authentication
        await auditLogger.LogActivityAsync(user.Email, $"User {user.UserName} successfully verified 2FA and logged in.");

        // Clear session and sign in the user
        HttpContext.Session.Remove("2FAUserId");
        await signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToPage("Index");
    }
}