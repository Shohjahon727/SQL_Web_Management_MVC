using SQL_Web_Management.Domain.Models;

namespace SQL_Web_Management.Application.Interfaces
{
	public interface ISchemaService
	{
		Task<DbObjectNode> GetRootAsync(int connectionId, string userId, CancellationToken cancellationToken = default);
		Task<IReadOnlyList<DbObjectNode>> GetDatabasesAsync(int connectionId, string userId, CancellationToken cancellationToken = default);
		Task<IReadOnlyList<DbObjectNode>> GetObjectsAsync(int connectionId, string userId, string database, string objectType, CancellationToken cancellationToken = default);
		Task<IReadOnlyList<TableColumnInfo>> GetTableColumnsAsync(int connectionId, string userId, string database, string schema, string table, CancellationToken cancellationToken = default);
		Task<string> GetSelectTopScriptAsync(string database, string schema, string table, int top = 1000);
	}
}
