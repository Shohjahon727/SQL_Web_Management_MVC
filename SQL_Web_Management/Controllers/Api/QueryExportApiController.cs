using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Domain.Models;
using System.Text;

namespace SQL_Web_Management.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/query")]
public class QueryExportApiController : ControllerBase
{
	[HttpPost("export-csv")]
	public IActionResult ExportCsv([FromBody] ExportQueryRequest request)
	{
		if (request.Columns.Count == 0)
		{
			return BadRequest(new { message = "Export qilish uchun ustunlar yo'q." });
		}

		var sb = new StringBuilder();
		sb.AppendLine(string.Join(",", request.Columns.Select(EscapeCsv)));

		foreach (var row in request.Rows)
		{
			var values = request.Columns.Select(col =>
			{
				row.TryGetValue(col, out var value);
				return EscapeCsv(value?.ToString() ?? string.Empty);
			});
			sb.AppendLine(string.Join(",", values));
		} 

		var bytes = Encoding.UTF8.GetBytes(sb.ToString());
		var fileName = $"{request.FileName}-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
		return File(bytes, "text/csv", fileName);
	}

	private static string EscapeCsv(string value)
	{
		if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
		{
			return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
		}
		return value;
	}
}
