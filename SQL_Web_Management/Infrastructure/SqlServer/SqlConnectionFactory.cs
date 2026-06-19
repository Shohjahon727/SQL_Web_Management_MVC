using Microsoft.Data.SqlClient;
using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Enums;
using SQL_Web_Management.Infrastructure.Security;

namespace SQL_Web_Management.Infrastructure.SqlServer
{
	public class SqlConnectionFactory : ISqlConnectionFactory
	{
		private readonly IConnectionSecretProtector _secretProtector;
		public SqlConnectionFactory(IConnectionSecretProtector secretProtector)
		{
			_secretProtector = secretProtector;
		}
		public string BuildConnectionString(string server, string database, AuthenticationType authenticationType, string username, string password)
		{
			var builder = new SqlConnectionStringBuilder
			{
				DataSource = server,
				InitialCatalog = database,
				TrustServerCertificate = true,
				Encrypt = true,
				ConnectTimeout = 15
			};

			if (authenticationType == AuthenticationType.Windows)
			{
				builder.IntegratedSecurity = true;
			}
			else
			{
				builder.UserID = username;
				builder.Password = password;
			}

			return builder.ConnectionString;
		}

		public SqlConnection CreateConnection(ConnectionProfile profile, string? databaseOverride = null)
		{
			var password = profile.AuthenticationType == AuthenticationType.Windows
			? string.Empty : _secretProtector.Unprotect(profile.EncryptedPassword);
			var connectionString = BuildConnectionString(
				profile.Server,
				databaseOverride ?? profile.Database,
				profile.AuthenticationType,
				profile.Username,
				password);
			return new SqlConnection(connectionString);
		}

		
		public async Task<SqlConnection> OpenConnectionAsync(ConnectionProfile profile, string? databaseOverride = null, CancellationToken cancellationToken = default)
		{
			var connection = CreateConnection(profile, databaseOverride);
			await connection.OpenAsync(cancellationToken);
			return connection;
		}
	}
}
