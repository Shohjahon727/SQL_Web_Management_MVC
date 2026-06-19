using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Models.ViewModels;

namespace SQL_Web_Management.Controllers
{
	public class WorkspaceController : Controller
	{
		private readonly IConnectionService _connectionService;
		private readonly UserManager<IdentityUser> _userManager;
		public WorkspaceController(IConnectionService connectionService, UserManager<IdentityUser> userManager)
		{
			_connectionService = connectionService;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index(int id, CancellationToken cancellationToken)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user is null) return Challenge();

			var profile = await _connectionService.GetByIdAsync(id,user.Id,cancellationToken);
			if (profile is null) return NotFound();

			await _connectionService.TouchLastUsedAsync(id, cancellationToken);
			return View(new WorkspaceViewModel
			{
				ConnectionId = profile.Id,
				ConnectionName = profile.Name,
				Server = profile.Server,
				Database = profile.Database
			});
		}
	}
}
