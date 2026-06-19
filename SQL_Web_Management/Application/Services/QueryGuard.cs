using System.Text.RegularExpressions;

namespace SQL_Web_Management.Application.Services
{
	public class QueryGuard
	{
		private static readonly string[] BlocedPatterns =
		[
			@"\bxp_cmdshell\b",
			@"\bsp_configure\b",
			@"\bxp_regread\b",
			@"\bDROP\s+DATABASE\b",
			@"\bBACKUP\s+DATABASE\b",
			@"\bRESTORE\s+DATABASE\b",
			@"\bSHUTDOWN\b",
			@"\bALTER\s+DATABASE\b"
		];

		public static (bool IsAllowed,string? Reason) Validate(string sql)
		{
			if (string.IsNullOrWhiteSpace(sql))
			{
				return (false, "Sql query bo'sh bo'lishi mumkin emas.");
			}

			foreach(var pattern in BlocedPatterns)
			{
				if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
				{
					return (false, $"Xavfsizlik sababli bu buyruq bloklangan: {pattern}");
				}
			}
			return (true, null);
		}
	}
}
