using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Domain.Entities;
using SQL_Web_Management.Domain.Enums;
using SQL_Web_Management.Models.ViewModels;

namespace SQL_Web_Management.Controllers
{
	[Authorize]
	public class ConnectionController : Controller
	{
		private readonly IConnectionService _connectionService;
		private readonly UserManager<IdentityUser> _userManager;
		public ConnectionController(IConnectionService connectionService, UserManager<IdentityUser> userManager)
		{
			_connectionService = connectionService;
			_userManager = userManager;
		}
		

		public async Task<IActionResult> Index(CancellationToken cancellationToken)
		{
			var userId = await GetUserIdAsync();
			var connections = await _connectionService.GettAllAsync(userId,cancellationToken);
			return View(connections);
		}

		public IActionResult Create() => View(new ConnectionFormViewModel());

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ConnectionFormViewModel model,CancellationToken cancellationToken)
		{
			ValidateAuthFields(model, ModelState);
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var userId = await GetUserIdAsync();
			var profile = MapToProfile(model,userId);

			await _connectionService.CreateAsync(profile, model.Password, cancellationToken);
			TempData["Success"] = "Ulanish saqlandi.";
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Edit(int id,CancellationToken cancellationToken)
		{
			var userId = await GetUserIdAsync();
			var profile = await _connectionService.GetByIdAsync(id, userId, cancellationToken);
			
			if (profile is null)
			{
				return NotFound();
			}
			var result = new ConnectionFormViewModel
			{
				Id = profile.Id,
				Name = profile.Name,
				Server = profile.Server,
				Database = profile.Database,
				AuthenticationType = profile.AuthenticationType,
				Username = profile.Username
			};
			return View(result);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, ConnectionFormViewModel model,CancellationToken cancellationToken)
		{
			if (id != model.Id)
			{
				return BadRequest();
			}

			ValidateAuthFields(model, ModelState);

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var userId = await GetUserIdAsync();
			var profile = await _connectionService.GetByIdAsync(id, userId, cancellationToken);

			if (profile is null)
			{
				return NotFound();
			}

			profile.Name = model.Name.Trim();
			profile.Server = model.Server.Trim();
			profile.Database = model.Database.Trim();
			profile.AuthenticationType = model.AuthenticationType;
			profile.Username = model.Username.Trim();

			await _connectionService.UpdateAsync(profile, model.Password, cancellationToken);
			TempData["Seccess"] = "Ulanish yangilandi";
			return RedirectToAction(nameof(Index));
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
		{
			var userId = await GetUserIdAsync();
			await _connectionService.DeleteAsync(id, userId, cancellationToken);
			TempData["Success"] = "Ulanish o'chirildi.";
			return RedirectToAction(nameof(Index));
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Test(int id, CancellationToken cancellationToken)
		{
			var userId = await GetUserIdAsync();
			var result = await _connectionService.TestSavedAsync(id, userId, cancellationToken);
			TempData[result.Success ? "Success" : "Error"] = result.Message;
			return RedirectToAction(nameof(Index));
		}
		private async Task<string> GetUserIdAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			return user?.Id ?? throw new InvalidOperationException("Foydalanuvchi topilmadi.");
		}
		private static void ValidateAuthFields(ConnectionFormViewModel model,ModelStateDictionary modelState)
		{
			if (model.AuthenticationType == AuthenticationType.SqlServer)
			{
				if (string.IsNullOrWhiteSpace(model.Username))
				{
					modelState.AddModelError(nameof(model.Username), "SQL Auth uchun username majburiy.");
				}
				if (model.Id == 0 && string.IsNullOrWhiteSpace(model.Password))
				{
					modelState.AddModelError(nameof(model.Password), "Yangi ulanish uchun parol majburiy.");
				}
			}
		}
		private static ConnectionProfile MapToProfile(ConnectionFormViewModel model, string userId) =>
			new()
			{
				Name = model.Name.Trim(),
				Server = model.Server.Trim(),
				Database = model.Database.Trim(),
				AuthenticationType = model.AuthenticationType,
				Username = model.AuthenticationType == AuthenticationType.Windows ? string.Empty : model.Username.Trim(),
				UserId = userId,
			};
	}


}
