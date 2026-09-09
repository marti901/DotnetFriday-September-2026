using Microsoft.AspNetCore.Mvc;
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

app.MapPost("/post", ([FromBody]Post post) =>
{
    return TypedResults.Ok();
})
.WithName("CreatePost");

app.Run();