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
        private readonly IDataProtector _protector;

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

        public async Task OnGetAsync()
        {
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
        }
    }
}
