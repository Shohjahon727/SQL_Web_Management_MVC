using System.ComponentModel.DataAnnotations;

namespace SQL_Web_Management.Models.ViewModels
{
	public class LoginViewModel
	{
		[Required]
		[Display(Name = "Email")]
		public string Email { get; set; } = string.Empty;

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Parol")]
		public string Password { get; set; } = string.Empty;
		public string? ReturnUrl { get; set; }
	}
}
