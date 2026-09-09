var builder = DistributedApplication.CreateBuilder(args);

// Redis is the broker behind the Dapr pub/sub component.
var redis = builder.AddRedis("redis");

var pubSub = builder
	.AddDaprPubSub("pubsub")
	.WithMetadata("redisHost", redis.GetEndpoint("tcp"));

// PostgreSQL database shared by the background processor (write) and the feed api (read).
var monkeyBookDb = builder
	.AddPostgres("postgres")
	.AddDatabase("monkeybookdb");

// Subscribes to the pub/sub topic and is the only writer of the database.
var backgroundProcessor = builder
	.AddProject<Projects.MoneyBook_BackgroundProcessor>("moneybook-backgroundprocessor")
	.WithReference(monkeyBookDb)
	.WaitFor(monkeyBookDb)
	.WaitFor(redis)
	.WithDaprSidecar(sidecar => sidecar.WithReference(pubSub));

// Publishes the post created events, has no database access.
builder
	.AddProject<Projects.MonkeyBook_PostsApi>("monkeybook-postsapi")
	.WaitFor(redis)
	.WithDaprSidecar(sidecar => sidecar.WithReference(pubSub));

// Only reads from the database, waits for the processor to apply the migrations.
builder
	.AddProject<Projects.MonkeyBook_FeedApi>("monkeybook-feedapi")
	.WithReference(monkeyBookDb)
	.WaitFor(backgroundProcessor);

builder.Build().Run();
