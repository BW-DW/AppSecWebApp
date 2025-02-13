using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
		[BindProperty]
		public Login LModel { get; set; }

		private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuditLoggerService auditLogger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuditLoggerService auditLogger)
		{
			this.signInManager = signInManager;
            this.userManager = userManager;
            this.auditLogger = auditLogger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Stop execution if model validation fails
            }

            // Get user from the database
            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return Page();
            }

            // Check if the account is locked out
            if (await userManager.IsLockedOutAsync(user))
            {
                await auditLogger.LogActivityAsync(LModel.Email, $"Locked out user {user.UserName} attempted to login.");
                ModelState.AddModelError("", "Your account is locked. Please try again later.");
                return Page();
            }

            // Attempt login using the correct username
            var signInResult = await signInManager.PasswordSignInAsync(user.UserName, LModel.Password, LModel.RememberMe, false);

            if (signInResult.Succeeded)
            {
                // Generate a new session ID
                string newSessionId = Guid.NewGuid().ToString();
                user.SessionId = newSessionId;
                await userManager.UpdateAsync(user);

                HttpContext.Session.SetString("UserEmail", user.Email);  // ✅ Store User Email in Session
                HttpContext.Session.SetString("UserId", user.Id);        // ✅ Store User ID in Session
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("SessionId", user.SessionId);

                await userManager.ResetAccessFailedCountAsync(user);
                await auditLogger.LogActivityAsync(LModel.Email, $"User {user.UserName} logged in successfully.");
                return RedirectToPage("Index");
            }

            // Increment failed attempt count
            await auditLogger.LogActivityAsync(LModel.Email, $"Failed login attempt for user {user.UserName}.");
            await userManager.AccessFailedAsync(user);

            if (await userManager.GetAccessFailedCountAsync(user) >= 3)
            {
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(5)); // Lockout for 5 mins
                await auditLogger.LogActivityAsync(LModel.Email, $"User {user.UserName} locked out due to too many failed attempts.");
                ModelState.AddModelError("", "Too many failed attempts. Your account is locked for 5 minutes.");
            }
            else
            {
                ModelState.AddModelError("", "Invalid email or password.");
            }

            return Page();
        }

        public void OnGet()
        {
        }
    }
}
