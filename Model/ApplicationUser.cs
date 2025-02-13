using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [DataType(DataType.Text)]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.CreditCard)]
        public string CreditCard { get; set; }

        //public string PhotoPath { get; set; } // Stores file path for JPG photo upload

        [Required]
        public string BillingAddress { get; set; }

        [Required]
        public string ShippingAddress { get; set; } // Special chars allowed

        [Required]
        public string? ProfilePicture { get; set; }

        public string? SessionId { get; set; }
    }
}
