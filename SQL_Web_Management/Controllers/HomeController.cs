using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQL_Web_Management.Models;
using System.Diagnostics;

namespace SQL_Web_Management.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index() => RedirectToAction("Index", "Connection");

		[AllowAnonymous]
		public IActionResult Privacy() => View();

		[AllowAnonymous]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel
			{
				RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
			});
		}
	}
}
