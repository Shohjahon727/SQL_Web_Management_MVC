using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Models.ViewModels;

namespace SQL_Web_Management.Controllers
{
	public class AccountController : Controller
	{
		private readonly SignInManager<IdentityUser> _signInManager;
		public AccountController(SignInManager<IdentityUser> signInManager)
		{
			_signInManager = signInManager;
		}

		[HttpGet]
		public IActionResult Login(string? returnUrl = null)
		{
			if (User.Identity?.IsAuthenticated == true)
			{
				return RedirectToAction("Index", "Connection");
			}
			return View(new LoginViewModel { ReturnUrl = returnUrl});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var result = await _signInManager.PasswordSignInAsync(model.Email,model.Password,
				isPersistent : true,lockoutOnFailure : false);

			if (result.Succeeded)
			{
				if(!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
				{
					return Redirect(model.ReturnUrl);
				}
				return RedirectToAction("Index", "Connection");
			}
			ModelState.AddModelError(string.Empty, "Email yoki parol noto'g'ri");
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction(nameof(Login));
		}
	}
}
