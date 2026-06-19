using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SQL_Web_Management.Infrastructure.Data
{
	public class DbInitializer
	{
		public static async Task InitializeAsync(IServiceProvider serviceProvider)
		{
			using var scope = serviceProvider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

			await context.Database.EnsureCreatedAsync();

			await EnsureSchemaUpdatedAsync(context);
			const string adminRole = "Admin";
			if (!await roleManager.RoleExistsAsync(adminRole))
			{
				await roleManager.CreateAsync(new IdentityRole(adminRole));
			}
			var email = configuration["DefaultAdmin:Email"] ?? "admin@local";
			var password = configuration["DefaultAdmin:Password"] ?? "Admin123!";

			var adminUser = await userManager.FindByEmailAsync(email);
			if (adminUser is null)
			{
				adminUser = new IdentityUser
				{
					UserName = email,
					Email = email,
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(adminUser, password);
				if (result.Succeeded)
				{
					await userManager.AddToRoleAsync(adminUser, adminRole);
				}
			}
		}

		private static async Task EnsureSchemaUpdatedAsync(AppDbContext context)
		{
			try
			{
				await context.Database.ExecuteSqlRawAsync(
					"ALTER TABLE ConnectionProfiles ADD COLUMN AuthenticationType INTEGER NOT NULL DEFAULT 0");
			}
			catch
			{
				// Column already exists on newer databases.
			}
		}
	}
}
