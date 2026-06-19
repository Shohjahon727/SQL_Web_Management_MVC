using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Application.Interfaces;

namespace SQL_Web_Management.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/explorer")]
public class ExplorerApiController : ControllerBase
{
	private readonly ISchemaService _schemaService;
	private readonly UserManager<IdentityUser> _userManager;

	public ExplorerApiController(ISchemaService schemaService, UserManager<IdentityUser> userManager)
	{
		_schemaService = schemaService;
		_userManager = userManager;
	}

	[HttpGet("{connectionId:int}/root")]
	public async Task<IActionResult> GetRoot(int connectionId,CancellationToken cancellationToken)
	{
		var userId = await GetUserIdAsync();
		var root = await _schemaService.GetRootAsync(connectionId,userId, cancellationToken);
		return Ok(root);
	}

	[HttpGet("{connectionId:int}/database/{database}/objects")]
	public async Task<IActionResult> GetObjacts(int connectionId,string database, [FromQuery] string type,CancellationToken cancellationToken)
	{
		var userId = await GetUserIdAsync();
		var objects = await _schemaService.GetObjectsAsync(connectionId,userId,database, type, cancellationToken);
		return Ok(objects);
	}

	[HttpGet("{connectionId:int}/database/{database}/table/{schema}/{table}/columns")]
	public async Task<IActionResult> GetColumns(int connectionId,string database,string schema,string table,CancellationToken cancellation)
	{
		var userId = await GetUserIdAsync();
		var columns = await _schemaService.GetTableColumnsAsync(connectionId,userId,database,schema,table,cancellation);
		return Ok(columns);
	}

	[HttpGet("{connectionId:int}/database/{database}/table/{schema}/{table}/select-top")]
	public async Task<IActionResult> GetSelectTop(int connectionId,string database,string schema,string table, [FromQuery] int top = 1000)
	{
		var script = await _schemaService.GetSelectTopScriptAsync(database,schema,table,top);
		return Ok(new { script });
	}
	private async Task<string> GetUserIdAsync()
	{
		var user = await _userManager.GetUserAsync(User);
		return user?.Id ?? throw new InvalidOperationException("Foydalanuvchi topilmadi.");
	}
}
