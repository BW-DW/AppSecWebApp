using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BookWorms.Model;

namespace BookWorms.Pages
{
    public class LogoutModel : PageModel
    {

		private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
		{
			this.signInManager = signInManager;
            this.userManager = userManager;
        }

		public async Task<IActionResult> OnPostLogoutAsync()
		{
            var userId = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.SessionId = null; // Clear stored session
                    await userManager.UpdateAsync(user);
                }
            }

            HttpContext.Session.Clear();
            await signInManager.SignOutAsync(); return RedirectToPage("Login");
		}

		public async Task<IActionResult> OnPostDontLogoutAsync()
		{
			return RedirectToPage("Index");
		}


		public void OnGet()
        {
        }
    }
}
