using Microsoft.AspNetCore.DataProtection;

namespace SQL_Web_Management.Infrastructure.Security
{
	public class ConnectionSecretProtector : IConnectionSecretProtector
	{
		private readonly IDataProtector _protector;
		public ConnectionSecretProtector(IDataProtectionProvider dataProtectionProvider)
		{
			_protector = dataProtectionProvider.CreateProtector("WebSqlManager.ConnectionSecrets.v1");
		}
		public string Protect(string plainText) => _protector.Protect(plainText);
		

		public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
		
	}
}
