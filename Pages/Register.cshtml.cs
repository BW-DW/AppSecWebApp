using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.ViewModels;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System;

namespace WebApplication1.Pages
{
    public class RegisterModel : PageModel
    {
        private UserManager<ApplicationUser> userManager { get; }
        private SignInManager<ApplicationUser> signInManager { get; }
        private readonly IWebHostEnvironment _environment; // ✅ To access `wwwroot`

        [BindProperty]
        public Register RModel { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment environment)
        {
            this.userManager = userManager; this.signInManager = signInManager; this._environment = environment;
        }

        public void OnGet()
        {
        }

        //Save data into the database
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var dataProtectionProvider = DataProtectionProvider.Create("EncryptData");
                var protector = dataProtectionProvider.CreateProtector("MySecretKey");

                string uniqueFileName = null;

                var existingUser = await userManager.FindByEmailAsync(RModel.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("RModel.Email", "This email is already registered.");
                    return Page();
                }

                // ✅ Validate password policy in backend
                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(userManager, null, RModel.Password);

                if (RModel.ProfilePicture != null)
                {
                    // ✅ Validate file extension (JPG only)
                    var allowedExtensions = new[] { ".jpg", ".jpeg" };
                    var extension = Path.GetExtension(RModel.ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("RModel.ProfilePicture", "Only JPG files are allowed.");
                        return Page();
                    }

                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images/profiles");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + RModel.ProfilePicture.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await RModel.ProfilePicture.CopyToAsync(fileStream);
                    }
                }

                var user = new ApplicationUser()
                {
                    UserName = RModel.FirstName + RModel.LastName,
                    Email = RModel.Email,
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    PhoneNumber = RModel.PhoneNumber,
                    BillingAddress = RModel.BillingAddress,
                    ShippingAddress = RModel.ShippingAddress,
                    CreditCard = protector.Protect(RModel.CreditCard),
                    ProfilePicture = uniqueFileName // ✅ Store filename in DB
                };
                var result = await userManager.CreateAsync(user, RModel.Password); 
                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, false);

                    // ✅ Create session after successful registration
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.UserName);

                    return RedirectToPage("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

            }
            return Page();
        }

    }
}
