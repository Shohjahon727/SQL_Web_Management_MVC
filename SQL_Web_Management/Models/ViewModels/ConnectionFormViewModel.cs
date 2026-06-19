using SQL_Web_Management.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SQL_Web_Management.Models.ViewModels
{
	public class ConnectionFormViewModel
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Nomi majburiy")]
		[Display(Name = "Nomi")]
		[StringLength(200)]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Server majburiy")]
		[Display(Name = "Server")]
		public string Server { get; set; } = "localhost";

		[Required(ErrorMessage = "Database majburiy")]
		[Display(Name = "Database")]
		public string Database { get; set; } = "master";

		[Display(Name = "Autentifikatsiya")]
		public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Windows;

		[Display(Name = "Username")]
		public string Username { get; set; } = "sa";
		[Display(Name = "Password")]
		[DataType(DataType.Password)]
		public string? Password { get; set; }
	}
}
