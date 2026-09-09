using Azure.Identity;
using Azure.Messaging.ServiceBus;
using MonkeyBook.PostsApi.Publishing;
using MonkeyBook.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddFrontendCors(builder.Configuration);

builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName));

var serviceBus = builder.Configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
	?? new ServiceBusOptions();

if (serviceBus.IsConfigured)
{
	// Azure, app service has no dapr sidecar so the managed identity sends to the topic directly.
	builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBus.Namespace, new DefaultAzureCredential()));
	builder.Services.AddSingleton<IPostPublisher, ServiceBusPostPublisher>();
}
else
{
	// Local, talks to the dapr sidecar that aspire runs next to this project.
	builder.Services.AddDaprClient();
	builder.Services.AddSingleton<IPostPublisher, DaprPostPublisher>();
}

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseFrontendCors();

// Publishes the new post, the background processor stores it in the database.
app.MapPost("/post", async (CreatePostRequest request, IPostPublisher publisher, CancellationToken cancellationToken) =>
{
	var post = new Post
	{
		Id = Guid.NewGuid(),
		MonkeyId = request.MonkeyId,
		Message = request.Message,
		Created = DateTime.UtcNow
	};

	await publisher.PublishAsync(post, cancellationToken);

	return TypedResults.Accepted($"/post/{post.Id}", post);
})
.WithName("CreatePost");

app.Run();

public record CreatePostRequest(Guid MonkeyId, string Message);
