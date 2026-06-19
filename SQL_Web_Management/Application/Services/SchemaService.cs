using Dapper;
using Microsoft.AspNetCore.Connections;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Domain.Enums;
using SQL_Web_Management.Domain.Models;
using SQL_Web_Management.Infrastructure.SqlServer;

namespace SQL_Web_Management.Application.Services
{
	public class SchemaService : ISchemaService
	{
		private readonly IConnectionService _connectionService;
		private readonly ISqlConnectionFactory _connectionFactory;

		public SchemaService(IConnectionService connectionService, ISqlConnectionFactory connectionFactory)
		{
			_connectionService = connectionService;
			_connectionFactory = connectionFactory;
		}

		public async Task<DbObjectNode> GetRootAsync(int connectionId, string userId, CancellationToken cancellationToken = default)
		{
			var profile = await _connectionService.GetByIdAsync(connectionId, userId, cancellationToken)
				?? throw new InvalidOperationException("Ulanish topilmadi.");

			var databases = (await GetDatabasesAsync(connectionId, userId, cancellationToken)).ToList();

			return new DbObjectNode
			{
				Id = $"server-{connectionId}",
				Name = profile.Server,
				Type = DbObjectType.Server,
				HasChildren = databases.Count > 0,
				Children = databases
			};
		}

		public async Task<IReadOnlyList<DbObjectNode>> GetDatabasesAsync(int connectionId, string userId, CancellationToken cancellationToken = default)
		{
			var profile = await _connectionService.GetByIdAsync(connectionId, userId, cancellationToken)
				?? throw new InvalidOperationException("Ulanish topilmadi.");

			await using var connection = await _connectionFactory.OpenConnectionAsync(profile, "master", cancellationToken);

			const string sql = """
			SELECT name
			FROM sys.databases
			WHERE state_desc = 'ONLINE'
			ORDER BY name
			""";

			var names = await connection.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: cancellationToken));

			return names.Select(name => new DbObjectNode
			{
				Id = $"db-{connectionId}-{name}",
				Name = name,
				Type = DbObjectType.Database,
				Database = name,
				HasChildren = true,
				Children =
				[
					new DbObjectNode
				{
					Id = $"tables-{connectionId}-{name}",
					Name = "Tables",
					Type = DbObjectType.TablesFolder,
					Database = name,
					HasChildren = true
				},
				new DbObjectNode
				{
					Id = $"views-{connectionId}-{name}",
					Name = "Views",
					Type = DbObjectType.ViewsFolder,
					Database = name,
					HasChildren = true
				},
				new DbObjectNode
				{
					Id = $"procs-{connectionId}-{name}",
					Name = "Stored Procedures",
					Type = DbObjectType.ProceduresFolder,
					Database = name,
					HasChildren = true
				}
				]
			}).ToList();
		}

		public async Task<IReadOnlyList<DbObjectNode>> GetObjectsAsync(int connectionId, string userId, string database, string objectType, CancellationToken cancellationToken = default)
		{
			var profile = await _connectionService.GetByIdAsync(connectionId, userId, cancellationToken)
				?? throw new InvalidOperationException("Ulanish topilmadi.");

			await using var connection = await _connectionFactory.OpenConnectionAsync(profile, database, cancellationToken);

			var sql = objectType.ToLowerInvariant() switch
			{
				"tables" => """
				SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name]
				FROM INFORMATION_SCHEMA.TABLES
				WHERE TABLE_TYPE = 'BASE TABLE'
				ORDER BY TABLE_SCHEMA, TABLE_NAME
				""",
				"views" => """
				SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name]
				FROM INFORMATION_SCHEMA.VIEWS
				ORDER BY TABLE_SCHEMA, TABLE_NAME
				""",
				"procedures" => """
				SELECT ROUTINE_SCHEMA AS [Schema], ROUTINE_NAME AS [Name]
				FROM INFORMATION_SCHEMA.ROUTINES
				WHERE ROUTINE_TYPE = 'PROCEDURE'
				ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME
				""",
				_ => throw new ArgumentException("Noto'g'ri object type.", nameof(objectType))
			};

			var rows = await connection.QueryAsync<(string Schema, string Name)>(new CommandDefinition(sql, cancellationToken: cancellationToken));

			var type = objectType.ToLowerInvariant() switch
			{
				"tables" => DbObjectType.Table,
				"views" => DbObjectType.View,
				"procedures" => DbObjectType.StoredProcedure,
				_ => DbObjectType.Table
			};

			return rows.Select(row => new DbObjectNode
			{
				Id = $"{objectType}-{connectionId}-{database}-{row.Schema}.{row.Name}",
				Name = $"{row.Schema}.{row.Name}",
				Type = type,
				Database = database,
				Schema = row.Schema,
				HasChildren = type == DbObjectType.Table
			}).ToList();
		}

		public async Task<IReadOnlyList<TableColumnInfo>> GetTableColumnsAsync(int connectionId, string userId, string database, string schema, string table, CancellationToken cancellationToken = default)
		{
			var profile = await _connectionService.GetByIdAsync(connectionId, userId, cancellationToken)
				?? throw new InvalidOperationException("Ulanish topilmadi.");

			await using var connection = await _connectionFactory.OpenConnectionAsync(profile, database, cancellationToken);

			const string sql = """
			SELECT
				c.COLUMN_NAME AS ColumnName,
				c.DATA_TYPE AS DataType,
				c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
				CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable,
				CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
			FROM INFORMATION_SCHEMA.COLUMNS c
			LEFT JOIN (
				SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
				FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
				INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
					ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
				WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
			) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
				AND c.TABLE_NAME = pk.TABLE_NAME
				AND c.COLUMN_NAME = pk.COLUMN_NAME
			WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @Table
			ORDER BY c.ORDINAL_POSITION
			""";

			var rows = await connection.QueryAsync<TableColumnInfo>(new CommandDefinition(
				sql,
				new { Schema = schema, Table = table },
				cancellationToken: cancellationToken));

			return rows.ToList();
		}

		public Task<string> GetSelectTopScriptAsync(string database, string schema, string table, int top = 1000)
		{
			var script = $"SELECT TOP ({top}) * FROM [{database}].[{schema}].[{table}];";
			return Task.FromResult(script);
		}
	}
}
