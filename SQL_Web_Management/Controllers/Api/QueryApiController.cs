using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Domain.Models;

namespace SQL_Web_Management.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/query")]
public class QueryApiController : ControllerBase
{
	private readonly IQueryService _queryService;
	private readonly UserManager<IdentityUser> _userManager;

	public QueryApiController(IQueryService queryService, UserManager<IdentityUser> userManager)
	{
		_queryService = queryService;
		_userManager = userManager;
	}

	[HttpPost("execute")]
	public async Task<IActionResult> Exucute([FromBody] ExecuteQueryRequest request,CancellationToken cancellationToken)
	{
		if (request.ConnectionId <= 0 || string.IsNullOrWhiteSpace(request.Sql) )
		{
			return BadRequest(new { message = "ConnectionId va SQL majburiy." });
		}

		var userId = await GetUserIdAsync();
		var result = await _queryService.ExecuteAsync(request.ConnectionId,userId,request.Sql,request.Database,cancellationToken);
		return Ok(result);
	}

	[HttpGet("history")]
	public async Task<IActionResult> History([FromQuery] int connectionId, [FromQuery] int take = 50,CancellationToken cancellationToken = default)
	{
		var userId = await GetUserIdAsync();
		var history = await _queryService.GetHistoryAsync(connectionId,userId,take,cancellationToken);
		return Ok(history);
	}

	private async Task<string> GetUserIdAsync()
	{
		var user = await _userManager.GetUserAsync(User);
		return user?.Id ?? throw new InvalidOperationException("Foydalanuvchi topilmadi.");
	}
}
