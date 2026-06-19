using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SQL_Web_Management.Application.Interfaces;
using SQL_Web_Management.Application.Services;
using SQL_Web_Management.Infrastructure.Data;
using SQL_Web_Management.Infrastructure.Security;
using SQL_Web_Management.Infrastructure.SqlServer;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});

builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseSqlite(builder.Configuration.GetConnectionString("AppDb"));
});

builder.Services
	.AddIdentity<IdentityUser, IdentityRole>(options =>
	{
		options.Password.RequireDigit = true;
		options.Password.RequireLowercase = true;
		options.Password.RequireUppercase = true;
		options.Password.RequireNonAlphanumeric = false;
		options.Password.RequiredLength = 8;
	}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
	options.AccessDeniedPath = "/Account/Login";
	options.Events.OnRedirectToLogin = context =>
	{
		if (context.Request.Path.StartsWithSegments("/api"))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return Task.CompletedTask;
		}

		context.Response.Redirect(context.RedirectUri);
		return Task.CompletedTask;
	};
	options.Events.OnRedirectToAccessDenied = context =>
	{
		if (context.Request.Path.StartsWithSegments("/api"))
		{
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			return Task.CompletedTask;
		}

		context.Response.Redirect(context.RedirectUri);
		return Task.CompletedTask;
	};
});

builder.Services.AddDataProtection();
builder.Services.AddScoped<IConnectionSecretProtector, ConnectionSecretProtector>();
builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IQueryService, QueryService>();

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
