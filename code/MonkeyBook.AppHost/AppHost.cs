var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MonkeyBook_FeedApi>("monkeybook-feedapi");

builder.AddProject<Projects.MonkeyBook_PostsApi>("monkeybook-postsapi");

builder.AddProject<Projects.MoneyBook_BackgroundProcessor>("moneybook-backgroundprocessor");

builder.Build().Run();
