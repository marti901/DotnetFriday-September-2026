using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MonkeyBook.Shared;

/// <summary>
/// Only used by "dotnet ef migrations" at design time, never at runtime.
/// </summary>
public class MonkeyBookDbContextFactory : IDesignTimeDbContextFactory<MonkeyBookDbContext>
{
	public MonkeyBookDbContext CreateDbContext(string[] args)
	{
		var options = new DbContextOptionsBuilder<MonkeyBookDbContext>()
			.UseNpgsql("Host=localhost;Database=monkeybookdb;Username=postgres;Password=postgres")
			.Options;

		return new MonkeyBookDbContext(options);
	}
}
