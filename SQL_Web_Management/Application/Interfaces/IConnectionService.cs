using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Enums;
using SQL_Web_Management.Domain.Models;

namespace SQL_Web_Management.Application.Interfaces
{
	public interface IConnectionService
	{
		Task<IReadOnlyList<ConnectionProfile>> GettAllAsync(string userId, CancellationToken cancellationToken = default);
		Task<ConnectionProfile?> GetByIdAsync(int id,  string userId,CancellationToken cancellationToken = default);
		Task<ConnectionProfile> CreateAsync(ConnectionProfile connectionProfile,string plainPassword, CancellationToken cancellationToken = default);
		Task UpdateAsync(ConnectionProfile profile,string plainPassword,CancellationToken cancellationToken = default);
		Task DeleteAsync(int id , string userId,CancellationToken cancellationToken = default);
		Task<ConnectionTestResult> TestAsync(string server, string database,AuthenticationType authenticationType,string username,string password,CancellationToken cancellationToken = default);
		Task<ConnectionTestResult> TestSavedAsync(int id, string userId,CancellationToken cancellationToken = default);
		Task TouchLastUsedAsync(int id,CancellationToken cancellationToken = default);
	}
}
