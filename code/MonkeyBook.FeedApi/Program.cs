using Microsoft.EntityFrameworkCore;
using MonkeyBook.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// This service only reads, so nothing needs to be tracked.
builder.AddNpgsqlDbContext<MonkeyBookDbContext>(
	"monkeybookdb",
	configureDbContextOptions: options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.MapGet("/feed", async (MonkeyBookDbContext dbContext) =>
{
	var feed = await dbContext.Posts
		.Join(dbContext.Monkeys, post => post.MonkeyId, monkey => monkey.Id, (post, monkey) => new { post, monkey })
		.OrderByDescending(x => x.post.Created)
		.Take(50)
		.Select(x => new FeedPost(
			x.post.Id,
			x.monkey.Id,
			x.monkey.Name,
			x.post.Message,
			x.post.Created))
		.ToListAsync();

	return TypedResults.Ok(feed);
})
.WithName("GetFeed");

// The frontend uses this to show the name of the monkey that is posting.
app.MapGet("/monkeys/{id:guid}", async (Guid id, MonkeyBookDbContext dbContext) =>
{
	var monkey = await dbContext.Monkeys
		.Where(monkey => monkey.Id == id)
		.Select(monkey => new MonkeyProfile(monkey.Id, monkey.Name))
		.FirstOrDefaultAsync();

	return monkey is null ? Results.NotFound() : Results.Ok(monkey);
})
.WithName("GetMonkey");

app.Run();

public record FeedPost(Guid Id, Guid MonkeyId, string MonkeyName, string Message, DateTime Created);

public record MonkeyProfile(Guid Id, string Name);
