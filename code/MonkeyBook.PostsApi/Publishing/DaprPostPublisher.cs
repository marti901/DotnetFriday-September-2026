using Dapr.Client;
using MonkeyBook.Shared;

namespace MonkeyBook.PostsApi.Publishing;

// Used when aspire runs the app locally, the dapr sidecar talks to redis.
public sealed class DaprPostPublisher(DaprClient daprClient, IConfiguration configuration) : IPostPublisher
{
	private readonly string topic = configuration[$"{ServiceBusOptions.SectionName}:Topic"] ?? "post-created";

	public Task PublishAsync(Post post, CancellationToken cancellationToken = default)
		=> daprClient.PublishEventAsync("pubsub", topic, post, cancellationToken);
}
