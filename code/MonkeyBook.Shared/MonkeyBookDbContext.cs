using Microsoft.EntityFrameworkCore;

namespace MonkeyBook.Shared;

public class MonkeyBookDbContext(DbContextOptions<MonkeyBookDbContext> options) : DbContext(options)
{
	public DbSet<Monkey> Monkeys => Set<Monkey>();

	public DbSet<Post> Posts => Set<Post>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Monkey>(monkey =>
		{
			monkey.HasKey(x => x.Id);
			monkey.Property(x => x.Name).HasMaxLength(100);
			monkey.HasMany(x => x.Posts).WithOne().HasForeignKey(x => x.MonkeyId);
		});

		modelBuilder.Entity<Post>(post =>
		{
			post.HasKey(x => x.Id);
			post.Property(x => x.Message).HasMaxLength(500);
			post.HasIndex(x => x.Created);
		});
	}
}
