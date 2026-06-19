using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Domain.Enums;
using SQL_Web_Management.Models.ViewModels;

namespace SQL_Web_Management.Controllers.Api
{
	[ApiController]
	[Route("api/connection")]
	public class ConnectionsApiController : ControllerBase
	{
		private readonly IConnectionService _connectionService;
		public ConnectionsApiController(IConnectionService connectionService)
		{
			_connectionService = connectionService;
		}

		[HttpPost("test")]
		public async Task<IActionResult> Test([FromBody] ConnectionFormViewModel model,CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(model.Server) || string.IsNullOrWhiteSpace(model.Database))
			{
				return BadRequest(new { message = "Server va Database to'ldirilishi shart." });
			}
			if (model.AuthenticationType == AuthenticationType.SqlServer)
			{
				if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
				{
					return BadRequest(new { message = "SQL Auth uchun Username va Password to'ldirilishi shart." });
				}
			}

			var result = await _connectionService.TestAsync(
				model.Server.Trim(),
				model.Database.Trim(),
				model.AuthenticationType,
				model.Username?.Trim() ?? string.Empty,
				model.Password ?? string.Empty,
				cancellationToken);

			return Ok(result);
		}

	}
}
