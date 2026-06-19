namespace SQL_Web_Management.Infrastructure.Security
{
	public interface IConnectionSecretProtector
	{
		string Protect(string plainText);
		string Unprotect(string protectedText);
	}
}
