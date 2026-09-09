using Dapr;
using MoneyBook.BackgroundProcessor;
using MoneyBook.BackgroundProcessor.Subscribing;
using MonkeyBook.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// This service is the only one that writes to the database.
builder.AddNpgsqlDbContext<MonkeyBookDbContext>("monkeybookdb");

builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName));

var serviceBus = builder.Configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
	?? new ServiceBusOptions();

if (serviceBus.IsConfigured)
{
	// Azure, app service has no dapr sidecar so we pull from the service bus subscription ourselves.
	builder.Services.AddHostedService<ServiceBusPostSubscriber>();
}

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

if (!serviceBus.IsConfigured)
{
	// Local, dapr delivers the events as cloud events and asks this endpoint for the subscriptions.
	app.UseCloudEvents();
	app.MapSubscribeHandler();

	app.MapPost("/post-created", [Topic("pubsub", "post-created")] async (
		Post post,
		MonkeyBookDbContext dbContext,
		ILogger<Program> logger,
		CancellationToken cancellationToken) =>
	{
		await PostStore.StoreAsync(post, dbContext, logger, cancellationToken);

		return Results.Ok();
	})
	.WithName("HandlePostCreated");
}

await MonkeyBookDatabase.MigrateAndSeedAsync(app.Services);

app.Run();
