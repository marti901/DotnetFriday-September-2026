using MonkeyBook.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var posts = new List<Post>
{
    new()
    {
        MonkeyId = Guid.NewGuid(),
        Message = "asdasdads asd asdad sads",
        Created = DateTime.UtcNow.AddDays(1)
    },
	new()
	{
		MonkeyId = Guid.NewGuid(),
		Message = "asdfikolu asdjklasd kjlasd",
		Created = DateTime.UtcNow.AddDays(1)
	},
	new()
	{
		MonkeyId = Guid.NewGuid(),
		Message = "aasd jklasD hjkasD asdjkhlhas dj",
		Created = DateTime.UtcNow.AddDays(1)
	}
};

app
	.MapGet("/feed", () => TypedResults.Ok(posts.OrderByDescending(x => x.Created)))
	.WithName("GetFeed");

app.Run();