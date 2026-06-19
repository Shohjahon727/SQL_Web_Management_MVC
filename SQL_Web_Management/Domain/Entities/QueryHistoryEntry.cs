namespace SQL_Web_Management.Domain.Entities
{
	public class QueryHistoryEntry
	{
		public int Id { get; set; }
		public int ConnectionProfileId { get; set; }
		public string Sql { get; set; } = string.Empty;
		public bool Success { get; set; }
		public string? ErrorMessage { get; set; }
		public int? RowsAffected { get; set; }
		public long ElapsedMs { get; set; }
		public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
		public string UserId { get; set; } = string.Empty;

		public ConnectionProfile? ConnectionProfile { get; set; }
	}
}
