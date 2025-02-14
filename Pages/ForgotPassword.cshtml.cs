using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text;
using System.Threading.Tasks;
using BookWorms.Model;
using BookWorms.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookWorms.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
		private readonly AuditLoggerService _auditLogger;

		public string Message { get; set; }

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, AuditLoggerService auditLogger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
			_auditLogger = auditLogger;
		}

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                ModelState.AddModelError("", "Please enter your email.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return Page();
            }

			// Log password reset action
			await _auditLogger.LogActivityAsync(user.Email, $"User {user.UserName} reset their password.");

			// Generate a new password
			string newPassword = GenerateRandomPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!resetResult.Succeeded)
            {
                ModelState.AddModelError("", "Error resetting password.");
                return Page();
            }

            // Send email with the new password
            string emailBody = $"Your new password is: <strong>{newPassword}</strong>. Please log in and change your password.";
            await _emailSender.SendEmailAsync(user.Email, "Password Reset", emailBody);

            Message = "A new password has been sent to your email.";
            return Page();
        }

        private string GenerateRandomPassword()
        {
            var options = _userManager.Options.Password;
            StringBuilder password = new StringBuilder();
            Random rand = new Random();

            for (int i = 0; i < options.RequiredLength; i++)
            {
                password.Append((char)rand.Next(33, 126)); // Generate random ASCII characters
            }

            return password.ToString();
        }
    }
}