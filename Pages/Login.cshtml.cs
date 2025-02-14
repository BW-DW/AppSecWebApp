using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BookWorms.Model;
using BookWorms.Services;
using BookWorms.ViewModels;
using Microsoft.Extensions.Configuration;

namespace BookWorms.Pages
{
    public class LoginModel : PageModel
    {
		[BindProperty]
		public Login LModel { get; set; }

		private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuditLoggerService auditLogger;
        private readonly IEmailSender emailSender;
		private readonly IHttpClientFactory httpClientFactory;
		private readonly IConfiguration configuration;

		private string RecaptchaSecretKey => configuration["GoogleReCaptcha:SecretKey"];
		public string RecaptchaSiteKey => configuration["GoogleReCaptcha:SiteKey"];


		public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuditLoggerService auditLogger, IEmailSender emailSender, IHttpClientFactory httpClientFactory,IConfiguration configuration)
		{
			this.signInManager = signInManager;
            this.userManager = userManager;
            this.auditLogger = auditLogger;
            this.emailSender = emailSender;
			this.httpClientFactory = httpClientFactory;
			this.configuration = configuration;
		}

		private async Task<bool> ValidateCaptcha(string recaptchaToken)
		{
			var client = httpClientFactory.CreateClient();
			var response = await client.PostAsync(
				$"https://www.google.com/recaptcha/api/siteverify?secret={RecaptchaSecretKey}&response={recaptchaToken}",
				null
			);

			var jsonResponse = await response.Content.ReadAsStringAsync();
			Console.WriteLine("Raw reCAPTCHA Response: " + jsonResponse); // ✅ Log raw JSON

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var recaptchaResult = JsonSerializer.Deserialize<RecaptchaResponse>(jsonResponse, options);

			if (recaptchaResult == null)
			{
				Console.WriteLine("Failed to deserialize reCAPTCHA response.");
				return false;
			}

			double captchaScore = 0.0;

			// ✅ Log everything for debugging
			Console.WriteLine("Success: " + recaptchaResult.Success);
			Console.WriteLine("Score: " + recaptchaResult.Score);
			Console.WriteLine("Action: " + recaptchaResult.Action);
			Console.WriteLine("Hostname: " + recaptchaResult.Hostname);
			Console.WriteLine("Challenge Timestamp: " + recaptchaResult.Challenge_ts);

			if (recaptchaResult.ErrorCodes != null && recaptchaResult.ErrorCodes.Length > 0)
			{
				Console.WriteLine("reCAPTCHA Errors: " + string.Join(", ", recaptchaResult.ErrorCodes));
			}

			if (recaptchaResult != null)
			{
				captchaScore = recaptchaResult.Score;
				ViewData["RecaptchaScore"] = $"reCAPTCHA Score: {captchaScore:F2}";
			}

			if (!recaptchaResult.Success)
			{
				Console.WriteLine("reCAPTCHA verification failed.");
				return false;
			}

			if (recaptchaResult.Score < 0.5)
			{
				Console.WriteLine($"reCAPTCHA score too low: {recaptchaResult.Score}");
				return false;
			}

			if (recaptchaResult.Action != "login")
			{
				Console.WriteLine($"Unexpected reCAPTCHA action: {recaptchaResult.Action}");
				return false;
			}

			Console.WriteLine("reCAPTCHA verification passed.");
			return true;
		}

		private class RecaptchaResponse
		{
			public bool Success { get; set; }
			public double Score { get; set; }
			public string Action { get; set; }
			public string Challenge_ts { get; set; } // ✅ Add missing field
			public string Hostname { get; set; } // ✅ Add missing field
			public string[] ErrorCodes { get; set; } // ✅ Capture errors if any
		}

		public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Stop execution if model validation fails
            }
            Console.WriteLine("Received reCAPTCHA token: " + LModel.RecaptchaToken);

            // Validate reCAPTCHA before continuing
            bool isHuman = await ValidateCaptcha(LModel.RecaptchaToken);
			if (!isHuman)
			{
				ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
				return Page();
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
            var signInResult = await signInManager.PasswordSignInAsync(user.UserName, LModel.Password, LModel.RememberMe, true);

			if (signInResult.RequiresTwoFactor)
			{
				Console.WriteLine("2FA required. Generating and sending token...");

				// Generate 2FA token
				var token2fa = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

				// Send 2FA token via email
				await emailSender.SendEmailAsync(user.Email, "Your 2FA Code", $"Your 2FA code is: {token2fa}");

				// Store user ID in session for verification
				HttpContext.Session.SetString("2FAUserId", user.Id);

				Console.WriteLine($"2FA token sent to {user.Email}");

				return RedirectToPage("Verify2FA"); // Redirect user to enter 2FA code
			}

			if (signInResult.Succeeded)
            {

                // ✅ Handle normal login (if 2FA is NOT enabled)
                string newSessionId = Guid.NewGuid().ToString();
                user.SessionId = newSessionId;
                await userManager.UpdateAsync(user);

                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("SessionId", user.SessionId);

                await userManager.ResetAccessFailedCountAsync(user);
                await auditLogger.LogActivityAsync(LModel.Email, $"User {user.UserName} logged in successfully.");

                return RedirectToPage("Index"); // ✅ Return here to stop execution
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
			ViewData["RecaptchaSiteKey"] = RecaptchaSiteKey;
		}
    }
}
