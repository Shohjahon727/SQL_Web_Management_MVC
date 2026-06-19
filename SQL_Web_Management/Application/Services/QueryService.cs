using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Models;
using SQL_Web_Management.Infrastructure.Data;
using SQL_Web_Management.Infrastructure.SqlServer;
using System.Diagnostics;

namespace SQL_Web_Management.Application.Services
{
	public class QueryService : IQueryService
	{
		private readonly AppDbContext _dbContext;
		private readonly IConnectionService _connectionService;
		private readonly ISqlConnectionFactory _connectionFactory;

		public QueryService(
			AppDbContext dbContext,
			IConnectionService connectionService,
			ISqlConnectionFactory connectionFactory)
		{
			_dbContext = dbContext;
			_connectionService = connectionService;
			_connectionFactory = connectionFactory;
		}
		public async Task<QueryExecuteResult> ExecuteAsync(int connectionId, string userId, string sql, string? database = null, CancellationToken cancellationToken = default)
		{
			var validation = QueryGuard.Validate(sql);
			if (!validation.IsAllowed)
			{
				return new QueryExecuteResult
				{
					Success = false,
					ErrorMessage = validation.Reason
				};
			}

			var profile = await _connectionService.GetByIdAsync(connectionId, userId, cancellationToken)
				?? throw new InvalidOperationException("Ulanish topilmadi.");

			var batches = SqlBatchSplitter.SplitBatches(sql);
			var sw = Stopwatch.StartNew();
			var messages = new List<string>();
			QueryExecuteResult? lastSelectResult = null;
			var totalRowsAffected = 0;

			try
			{
				await using var connection = await _connectionFactory.OpenConnectionAsync(
					profile,
					databaseOverride: string.IsNullOrWhiteSpace(database) ? null : database.Trim(),
					cancellationToken: cancellationToken);

				for (var i = 0; i < batches.Count; i++)
				{
					var batch = batches[i];
					var batchLabel = batches.Count > 1 ? $"Batch {i + 1}/{batches.Count}: " : string.Empty;

					await using var command = connection.CreateCommand();
					command.CommandText = batch;
					command.CommandTimeout = 120;

					if (IsSelectQuery(batch))
					{
						var result = await ExecuteSelectAsync(command, cancellationToken);
						lastSelectResult = result;
						messages.Add($"{batchLabel}{result.Rows.Count} qator qaytdi.");
						continue;
					}

					var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
					if (rowsAffected >= 0)
					{
						totalRowsAffected += rowsAffected;
					}

					messages.Add($"{batchLabel}{FormatNonQueryMessage(batch, rowsAffected)}");
				}

				sw.Stop();

				var finalResult = lastSelectResult ?? new QueryExecuteResult();
				finalResult.Success = true;
				finalResult.ElapsedMs = sw.ElapsedMilliseconds;
				finalResult.Message = string.Join("\n", messages);
				finalResult.RowsAffected = totalRowsAffected > 0 ? totalRowsAffected : null;

				if (lastSelectResult is null)
				{
					finalResult.Columns = [];
					finalResult.Rows = [];
				}

				await SaveHistoryAsync(connectionId, userId, sql, true, null, totalRowsAffected, sw.ElapsedMilliseconds, cancellationToken);
				await _connectionService.TouchLastUsedAsync(connectionId, cancellationToken);
				return finalResult;
			}
			catch (Exception ex)
			{
				sw.Stop();
				await SaveHistoryAsync(connectionId, userId, sql, false, ex.Message, null, sw.ElapsedMilliseconds, cancellationToken);

				return new QueryExecuteResult
				{
					Success = false,
					ErrorMessage = ex.Message,
					ElapsedMs = sw.ElapsedMilliseconds,
					Message = messages.Count > 0 ? string.Join("\n", messages) : null
				};
			}
		}

		public async Task<IReadOnlyList<QueryHistoryEntry>> GetHistoryAsync(int connectionId, string userId, int take = 50, CancellationToken cancellationToken = default)
		{
			return await _dbContext.QueryHistory
				.AsNoTracking()
				.Where(h => h.ConnectionProfileId == connectionId && h.UserId == userId)
				.OrderByDescending(h => h.ExecutedAt)
				.Take(take)
				.ToListAsync(cancellationToken);
		}

		private static string FormatNonQueryMessage(string batch, int rowsAffected)
		{
			if (batch.TrimStart().StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase))
			{
				return "Database muvaffaqiyatli yaratildi.";
			}

			if (batch.TrimStart().StartsWith("DROP DATABASE", StringComparison.OrdinalIgnoreCase))
			{
				return "Database o'chirildi.";
			}

			return rowsAffected switch
			{
				-1 => "Buyruq muvaffaqiyatli bajarildi.",
				0 => "0 qator ta'sirlandi.",
				_ => $"{rowsAffected} qator ta'sirlandi."
			};
		}

		private static bool IsSelectQuery(string sql)
		{
			var trimmed = sql.TrimStart();
			return trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
				|| trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)
				|| trimmed.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase);
		}

		private static async Task<QueryExecuteResult> ExecuteSelectAsync(SqlCommand command, CancellationToken cancellationToken)
		{
			await using var reader = await command.ExecuteReaderAsync(cancellationToken);

			var columns = Enumerable.Range(0, reader.FieldCount)
				.Select(reader.GetName)
				.ToList();

			var rows = new List<Dictionary<string, object?>>();

			while (await reader.ReadAsync(cancellationToken))
			{
				var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
				for (var i = 0; i < reader.FieldCount; i++)
				{
					var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
					row[columns[i]] = value is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : value;
				}
				rows.Add(row);
			}

			return new QueryExecuteResult
			{
				Columns = columns,
				Rows = rows
			};
		}

		private async Task SaveHistoryAsync(
			int connectionId,
			string userId,
			string sql,
			bool success,
			string? errorMessage,
			int? rowsAffected,
			long elapsedMs,
			CancellationToken cancellationToken)
		{
			_dbContext.QueryHistory.Add(new QueryHistoryEntry
			{
				ConnectionProfileId = connectionId,
				UserId = userId,
				Sql = sql.Length > 4000 ? sql[..4000] : sql,
				Success = success,
				ErrorMessage = errorMessage,
				RowsAffected = rowsAffected,
				ElapsedMs = elapsedMs,
				ExecutedAt = DateTime.UtcNow
			});

			await _dbContext.SaveChangesAsync(cancellationToken);
		}
	}
}
