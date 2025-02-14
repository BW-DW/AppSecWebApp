using System.ComponentModel.DataAnnotations;

namespace BookWorms.ViewModels
{
    public class Login
    {
        [Required][DataType(DataType.EmailAddress)] public string Email { get; set; }
        [Required][DataType(DataType.Password)] public string Password { get; set; }
        public bool RememberMe { get; set; }

		public string RecaptchaToken { get; set; } 
	}
}
