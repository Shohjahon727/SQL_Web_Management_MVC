using Microsoft.Data.SqlClient;
using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Enums;

namespace SQL_Web_Management.Infrastructure.SqlServer
{
	public interface ISqlConnectionFactory
	{
		SqlConnection CreateConnection(ConnectionProfile profile, string? databaseOverride = null);
		Task<SqlConnection> OpenConnectionAsync(
			ConnectionProfile profile,
			string? databaseOverride = null,
			CancellationToken cancellationToken = default
			);
		string BuildConnectionString(
			string server,
			string database,
			AuthenticationType authenticationType,
			string username,
			string password);
	}
}
