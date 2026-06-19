namespace SQL_Web_Management.Domain.Models
{
	public class QueryExecuteResult
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
		public string? ErrorMessage { get; set; }
		public int? RowsAffected { get; set; }
		public long ElapsedMs { get; set; }
		public List<string> Columns { get; set; } = [];
		public List<Dictionary<string, object?>> Rows { get; set; } = [];
	}
}
