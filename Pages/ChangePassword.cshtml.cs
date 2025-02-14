using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using BookWorms.Model;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace BookWorms.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthDbContext _dbContext;

        public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AuthDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
        }

        [BindProperty]
        public ChangePasswordInputModel ChangePassword { get; set; }

        public class ChangePasswordInputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string OldPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "New passwords do not match.")]
            public string ConfirmNewPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            // Get the last password change record
            var lastPasswordChange = await _dbContext.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.CreatedAt)
                .FirstOrDefaultAsync();

            // Enforce minimum password age (e.g., 5 minutes)
            if (lastPasswordChange != null && (DateTime.UtcNow - lastPasswordChange.CreatedAt).TotalMinutes < 5)
            {
                ModelState.AddModelError(string.Empty, "You must wait at least 5 minutes before changing your password again.");
                return Page();
            }

            // Enforce maximum password age (e.g., 30 days)
            if (lastPasswordChange != null && (DateTime.UtcNow - lastPasswordChange.CreatedAt).TotalDays > 30)
            {
                ModelState.AddModelError(string.Empty, "Your password has expired. Please change it immediately.");
                return Page();
            }

            // Check password history
            var previousPasswords = await _dbContext.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.CreatedAt)
                .Take(2) // Get the last 2 passwords
                .ToListAsync();

            foreach (var oldPassword in previousPasswords)
            {
                if (_userManager.PasswordHasher.VerifyHashedPassword(user, oldPassword.HashedPassword, ChangePassword.NewPassword) == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty, "You cannot reuse your last 2 passwords.");
                    return Page();
                }
            }

            // Validate password strength
            if (!ValidatePassword(ChangePassword.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "New password must be at least 12 characters long and include at least one uppercase letter, one lowercase letter, one number, and one special character.");
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, ChangePassword.OldPassword, ChangePassword.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            user.PasswordLastChanged = DateTime.UtcNow; // Update password change timestamp

            // Save new password to history
            var hashedNewPassword = _userManager.PasswordHasher.HashPassword(user, ChangePassword.NewPassword);
            _dbContext.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                HashedPassword = hashedNewPassword
            });

            // Keep only the last 2 passwords
            if (previousPasswords.Count == 2)
            {
                _dbContext.PasswordHistories.Remove(previousPasswords.Last()); // Remove the oldest
            }

            await _dbContext.SaveChangesAsync(); //  Ensure changes are saved to the database

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully.";
            return Page();
        }

        private bool ValidatePassword(string password)
        {
            // Ensure password meets complexity requirements
            var hasUpperCase = password.Any(char.IsUpper);
            var hasLowerCase = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*(),.?\:{ }|<>]");

            return password.Length >= 12 && hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }
    }
}