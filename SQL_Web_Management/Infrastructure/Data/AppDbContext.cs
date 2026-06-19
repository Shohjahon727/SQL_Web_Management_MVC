using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SQL_Web_Management.Domain.Entities;

namespace SQL_Web_Management.Infrastructure.Data
{
	public class AppDbContext : IdentityDbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}
		public DbSet<ConnectionProfile> ConnectionProfiles => Set<ConnectionProfile>();
		public DbSet<QueryHistoryEntry> QueryHistory => Set<QueryHistoryEntry>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<ConnectionProfile>(entity =>
			{
				entity.HasKey(x => x.Id);
				entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
				entity.Property(x => x.Server).HasMaxLength(500).IsRequired();
				entity.Property(x => x.Database).HasMaxLength(200).IsRequired();
				entity.Property(x => x.AuthenticationType).HasDefaultValue(Domain.Enums.AuthenticationType.SqlServer);
				entity.Property(x => x.Username).HasMaxLength(200).IsRequired();
				entity.Property(x => x.EncryptedPassword).IsRequired();
				entity.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
			});

			modelBuilder.Entity<QueryHistoryEntry>(entity =>
			{
				entity.HasKey(x => x.Id);
				entity.Property(x => x.Sql).IsRequired();
				entity.HasOne(x => x.ConnectionProfile).WithMany()
					.HasForeignKey(x => x.ConnectionProfileId).OnDelete(DeleteBehavior.Cascade);
				entity.HasIndex(x => new { x.UserId, x.ExecutedAt });
			});
		}
	}
}
