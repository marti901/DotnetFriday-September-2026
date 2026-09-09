using Dapr.Client;
using MonkeyBook.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// Talks to the Dapr sidecar that Aspire runs next to this project.
builder.Services.AddDaprClient();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

// Publishes the new post, the background processor stores it in the database.
app.MapPost("/post", async (CreatePostRequest request, DaprClient daprClient) =>
{
	var post = new Post
	{
		Id = Guid.NewGuid(),
		MonkeyId = request.MonkeyId,
		Message = request.Message,
		Created = DateTime.UtcNow
	};

	await daprClient.PublishEventAsync("pubsub", "post-created", post);

	return TypedResults.Accepted($"/post/{post.Id}", post);
})
.WithName("CreatePost");

app.Run();

public record CreatePostRequest(Guid MonkeyId, string Message);
