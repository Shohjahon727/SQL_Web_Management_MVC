namespace SQL_Web_Management.Domain.Models
{
	public class ExportQueryRequest
	{
		public List<string> Columns { get; set; } = [];
		public List<Dictionary<string, object?>> Rows { get; set; } = [];
		public string FileName { get; set; } = "query-result";
	}
}
