using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebApplication1.Model;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
		[BindProperty]
		public Login LModel { get; set; }

		private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
		{
			this.signInManager = signInManager;
            this.userManager = userManager;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Stop execution if model validation fails
            }

            // ✅ Get user from the database
            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return Page();
            }

            // ✅ Attempt login using the correct username
            var signInResult = await signInManager.PasswordSignInAsync(user.UserName, LModel.Password, LModel.RememberMe, false);

            if (signInResult.Succeeded)
            {
                return RedirectToPage("Index"); // Redirect on success
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return Page();
        }

        public void OnGet()
        {
        }
    }
}
