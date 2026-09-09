using Dapr;
using Microsoft.EntityFrameworkCore;
using MoneyBook.BackgroundProcessor;
using MonkeyBook.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// This service is the only one that writes to the database.
builder.AddNpgsqlDbContext<MonkeyBookDbContext>("monkeybookdb");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

// Dapr delivers the events as cloud events and asks this endpoint for the subscriptions.
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapPost("/post-created", [Topic("pubsub", "post-created")] async (
	Post post,
	MonkeyBookDbContext dbContext,
	ILogger<Program> logger) =>
{
	if (await dbContext.Posts.AnyAsync(x => x.Id == post.Id))
	{
		logger.LogInformation("Post {PostId} was already stored, skipping.", post.Id);
		return Results.Ok();
	}

	dbContext.Posts.Add(post);
	await dbContext.SaveChangesAsync();

	logger.LogInformation("Stored post {PostId} of monkey {MonkeyId}.", post.Id, post.MonkeyId);

	return Results.Ok();
})
.WithName("HandlePostCreated");

await MonkeyBookDatabase.MigrateAndSeedAsync(app.Services);

app.Run();
