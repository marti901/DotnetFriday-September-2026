using Microsoft.EntityFrameworkCore;
using MonkeyBook.Shared;

namespace MoneyBook.BackgroundProcessor;

// The single place that stores a post, used by the dapr endpoint locally and by the service bus subscriber in azure.
public static class PostStore
{
	public static async Task StoreAsync(
		Post post,
		MonkeyBookDbContext dbContext,
		ILogger logger,
		CancellationToken cancellationToken = default)
	{
		if (await dbContext.Posts.AnyAsync(x => x.Id == post.Id, cancellationToken))
		{
			logger.LogInformation("Post {PostId} was already stored, skipping.", post.Id);
			return;
		}

		dbContext.Posts.Add(post);
		await dbContext.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Stored post {PostId} of monkey {MonkeyId}.", post.Id, post.MonkeyId);
	}
}
