using SQL_Web_Management.Domain.Enums;

namespace SQL_Web_Management.Domain.Entities
{
	public class ConnectionProfile
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Server { get; set; } = string.Empty;
		public string Database { get; set; } = "master";
		public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.SqlServer;
		public string? Username { get; set; }
		public string? EncryptedPassword { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? LastUseAt { get; set; }
		public string UserId { get; set; } = string.Empty;
	}
}
