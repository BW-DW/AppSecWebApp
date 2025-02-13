using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IDataProtector _protector;
        private readonly IHttpContextAccessor contxt;


        public ApplicationUser CurrentUser { get; set; }
        public string DecryptedCreditCard { get; set; }
        public string HashedPassword { get; set; }

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            var dataProtectionProvider = DataProtectionProvider.Create("EncryptData");
            _protector = dataProtectionProvider.CreateProtector("MySecretKey");
        }

        //public IndexModel(ILogger<IndexModel> logger)
        //{
        //    _logger = logger;
        //}

        //public void OnGet()
        //{

        //}

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var sessionId = HttpContext.Session.GetString("SessionId");
            Console.WriteLine("userId", userId, "| sessionid", sessionId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            {
                return RedirectToPage("Login"); // Redirect if session expired
            }

            if (HttpContext.Session.GetString("UserEmail") == null)  // ✅ Check if session exists
            {
                return RedirectToPage("Login");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.SessionId != sessionId)
            {
                HttpContext.Session.Clear(); // Invalidate session
                return RedirectToPage("Login"); // Redirect if session is invalid
            }

            if (User.Identity.IsAuthenticated)
            {
                CurrentUser = await _userManager.GetUserAsync(User);

                if (CurrentUser?.CreditCard != null)
                {
                    try
                    {
                        DecryptedCreditCard = _protector.Unprotect(CurrentUser.CreditCard);
                    }
                    catch
                    {
                        DecryptedCreditCard = "Error decrypting credit card";
                    }
                }

                if (CurrentUser != null)
                {
                    HashedPassword = CurrentUser.PasswordHash ?? "Password hash not available";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostToggle2FAAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.TwoFactorEnabled = !user.TwoFactorEnabled;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToPage();
        }
    }
}
