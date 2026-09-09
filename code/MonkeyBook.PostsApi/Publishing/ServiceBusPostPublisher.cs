using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using MonkeyBook.Shared;

namespace MonkeyBook.PostsApi.Publishing;

// Used in azure, app service has no dapr sidecar so the post goes straight to a service bus topic.
public sealed class ServiceBusPostPublisher : IPostPublisher, IAsyncDisposable
{
	private readonly ServiceBusSender sender;

	public ServiceBusPostPublisher(ServiceBusClient client, IOptions<ServiceBusOptions> options)
	{
		sender = client.CreateSender(options.Value.Topic);
	}

	public async Task PublishAsync(Post post, CancellationToken cancellationToken = default)
	{
		var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(post))
		{
			ContentType = "application/json",
			MessageId = post.Id.ToString()
		};

		await sender.SendMessageAsync(message, cancellationToken);
	}

	public ValueTask DisposeAsync() => sender.DisposeAsync();
}
