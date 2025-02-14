using System.ComponentModel.DataAnnotations;

namespace BookWorms.ViewModels
{
    public class Register
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [DataType(DataType.EmailAddress)] public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        [DataType(DataType.Password)] public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation password does not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First Name can only contain letters.")]
        [MaxLength(50, ErrorMessage = "First Name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Last Name can only contain letters.")]
        [MaxLength(50, ErrorMessage = "Last Name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression(@"^[0-9]{8,15}$", ErrorMessage = "Phone Number must be between 8 and 15 digits.")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Credit Card Number is required.")]
        [DataType(DataType.CreditCard)]
        public string CreditCard { get; set; }

        [Required(ErrorMessage = "Billing Address is required.")]
        [MaxLength(200, ErrorMessage = "Billing Address cannot exceed 200 characters.")]
        public string BillingAddress { get; set; }

        [Required(ErrorMessage = "Shipping Address is required.")]
        [MaxLength(200, ErrorMessage = "Shipping Address cannot exceed 200 characters.")]
        public string ShippingAddress { get; set; } // ✅ Allow all special characters

        [Required]
        public IFormFile? ProfilePicture { get; set; }
    }

}
