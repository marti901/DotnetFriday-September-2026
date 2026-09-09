using Microsoft.EntityFrameworkCore;
using MonkeyBook.Shared;

namespace MoneyBook.BackgroundProcessor;

public static class MonkeyBookDatabase
{
	private static readonly Monkey[] Monkeys =
	[
		new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "George" },
		new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Kong" },
		new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Caesar" }
	];

	public static async Task MigrateAndSeedAsync(IServiceProvider services)
	{
		using var scope = services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<MonkeyBookDbContext>();

		await dbContext.Database.MigrateAsync();

		var existingIds = await dbContext.Monkeys.Select(monkey => monkey.Id).ToListAsync();
		var missingMonkeys = Monkeys.Where(monkey => !existingIds.Contains(monkey.Id)).ToList();

		if (missingMonkeys.Count == 0)
		{
			return;
		}

		dbContext.Monkeys.AddRange(missingMonkeys);
		await dbContext.SaveChangesAsync();
	}
}
