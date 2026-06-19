using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Models;

namespace SQL_Web_Management.Application.Interfaces
{
	public interface IQueryService
	{
		Task<QueryExecuteResult> ExecuteAsync(int connectionId, string userId, string sql, string? database = null, CancellationToken cancellationToken = default);
		Task<IReadOnlyList<QueryHistoryEntry>> GetHistoryAsync(int connectionId, string userId, int take = 50, CancellationToken cancellationToken = default);
	}
}
