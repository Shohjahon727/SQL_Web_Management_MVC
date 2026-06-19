namespace SQL_Web_Management.Domain.Models
{
	public class ExecuteQueryRequest
	{
		public int ConnectionId { get; set; }
		public string Sql { get; set; } = string.Empty;
		public string? Database { get; set; }
	}
}
