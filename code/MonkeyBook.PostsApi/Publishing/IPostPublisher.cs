using MonkeyBook.Shared;

namespace MonkeyBook.PostsApi.Publishing;

// Dapr locally, azure service bus in azure.
public interface IPostPublisher
{
	Task PublishAsync(Post post, CancellationToken cancellationToken = default);
}
