using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Enums;
using SQL_Web_Management.Domain.Models;
using SQL_Web_Management.Infrastructure.Data;
using SQL_Web_Management.Infrastructure.Security;
using SQL_Web_Management.Infrastructure.SqlServer;
using System.Diagnostics;

namespace SQL_Web_Management.Application.Services;

public class ConnectionService : IConnectionService
{
	private readonly AppDbContext _dbContext;
	private readonly ISqlConnectionFactory _connectionFactory;
	private readonly IConnectionSecretProtector _connectionSecretProtector;

	public ConnectionService(AppDbContext dbContext, ISqlConnectionFactory connectionFactory, IConnectionSecretProtector connectionSecretProtector)
	{
		_connectionFactory = connectionFactory;
		_connectionSecretProtector = connectionSecretProtector;
		_dbContext = dbContext;
	}
	public async Task<ConnectionProfile> CreateAsync(ConnectionProfile connectionProfile, string plainPassword, CancellationToken cancellationToken = default)
	{
		connectionProfile.EncryptedPassword = connectionProfile.AuthenticationType == AuthenticationType.Windows
			? _connectionSecretProtector.Protect(string.Empty)
			: _connectionSecretProtector.Protect(plainPassword ?? string.Empty);

		connectionProfile.CreatedAt = DateTime.UtcNow;

		_dbContext.ConnectionProfiles.Add(connectionProfile);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return connectionProfile;
	}

	public async Task DeleteAsync(int id, string userId, CancellationToken cancellationToken = default)
	{
		var existing = await _dbContext.ConnectionProfiles
			.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

		if (existing is null)
		{
			return;
		}

		_dbContext.ConnectionProfiles.Remove(existing);
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task<ConnectionProfile?> GetByIdAsync(int id, string userId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.ConnectionProfiles.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
	}

	public async Task<IReadOnlyList<ConnectionProfile>> GettAllAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.ConnectionProfiles.AsNoTracking().
			Where(c => c.UserId == userId).
			OrderByDescending(c => c.LastUseAt ?? c.CreatedAt).
			ToListAsync(cancellationToken);
	}

	public async Task<ConnectionTestResult> TestAsync(
		string server, string database, AuthenticationType authenticationType,
		string username, string password, CancellationToken cancellationToken = default)
	{
		var sw = Stopwatch.StartNew();

		try
		{
			var connectionString = _connectionFactory.BuildConnectionString(
				server, database, authenticationType, username, password);

			await using var connection = new SqlConnection(connectionString);
			await connection.OpenAsync(cancellationToken);

			sw.Stop();
			var authLabel = authenticationType == AuthenticationType.Windows ? "Windows Auth" : username;

			return new ConnectionTestResult
			{
				Success = true,
				Message = $"Ulanish muvaffaqiyatli: {server} / {database} ({authLabel})",
				ElapsedMs = sw.ElapsedMilliseconds
			};
		}
		catch (Exception ex)
		{
			sw.Stop();
			return new ConnectionTestResult
			{
				Success = false,
				Message = FormatConnectionError(ex, authenticationType, server),
				ElapsedMs = sw.ElapsedMilliseconds
			};
		}
	}

	public async Task<ConnectionTestResult> TestSavedAsync(int id, string userId, CancellationToken cancellationToken = default)
	{
		var profile = await GetByIdAsync(id, userId, cancellationToken) ?? throw new InvalidOperationException("Ulanish topilmadi");
		var password = profile.AuthenticationType == AuthenticationType.Windows ? string.Empty :
			_connectionSecretProtector.Unprotect(profile.EncryptedPassword);

		return await TestAsync(profile.Server, profile.Database, profile.AuthenticationType, profile.Username, password, cancellationToken);
	}

	public async Task TouchLastUsedAsync(int id, CancellationToken cancellationToken = default)
	{
		var existing = await _dbContext.ConnectionProfiles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
		if (existing is null)
		{
			return;
		}

		existing.LastUseAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task UpdateAsync(ConnectionProfile profile, string plainPassword, CancellationToken cancellationToken = default)
	{
		var existing = await _dbContext.ConnectionProfiles.FirstOrDefaultAsync(c => c.Id == profile.Id && c.UserId == profile.UserId, cancellationToken)
		?? throw new InvalidOperationException("Ulanish topilmadi.");

		existing.Name = profile.Name;
		existing.Server = profile.Server;
		existing.AuthenticationType = profile.AuthenticationType;
		existing.Database = profile.Database;
		existing.Username = profile.Username;

		if (profile.AuthenticationType == AuthenticationType.Windows)
		{
			existing.EncryptedPassword = _connectionSecretProtector.Protect(string.Empty);
		}
		else if (!string.IsNullOrWhiteSpace(plainPassword))
		{
			existing.EncryptedPassword = _connectionSecretProtector.Protect(plainPassword);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}
	private static string FormatConnectionError(Exception ex, AuthenticationType authenticationType, string server)
	{
		var message = ex.Message;
		if (message.Contains("Login failed for user", StringComparison.OrdinalIgnoreCase))
		{
			if (authenticationType == AuthenticationType.SqlServer)
			{
				return $"{message}\n\nMaslahat:\n" +
					"1) 'Windows Authentication' ni tanlab ko'ring (ko'p hollarda ishlaydi)\n" +
					"2) SSMS → Server Properties → Security → 'SQL Server and Windows Authentication mode'\n" +
					"3) 'sa' parolini to'g'rilang yoki yangi SQL user yarating\n" +
					$"4) Server nomini tekshiring: localhost, localhost\\SQLEXPRESS, (localdb)\\MSSQLLocalDB";
			}

			return $"{message}\n\nMaslahat: Windows Authentication uchun ilovani administrator sifatida emas, o'z Windows akkauntingiz bilan ishlating.";
		}

		if (message.Contains("network-related", StringComparison.OrdinalIgnoreCase) || message.Contains("server was not found", StringComparison.OrdinalIgnoreCase))
		{
			return $"{message}\n\nMaslahat: Server nomini tekshiring.\n" +
				$"- Hozirgi: {server}\n" +
				"- Default instance: localhost\n" +
				"- Express: localhost\\SQLEXPRESS\n" +
				"- LocalDB: (localdb)\\MSSQLLocalDB";
		}
		return message;
	}
}
