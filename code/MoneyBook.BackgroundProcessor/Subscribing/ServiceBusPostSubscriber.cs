using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using MonkeyBook.Shared;

namespace MoneyBook.BackgroundProcessor.Subscribing;

// Replaces the dapr subscription when the app runs on azure app service.
public sealed class ServiceBusPostSubscriber(
	IOptions<ServiceBusOptions> options,
	IServiceProvider services,
	ILogger<ServiceBusPostSubscriber> logger) : BackgroundService
{
	private ServiceBusClient? client;
	private ServiceBusProcessor? processor;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var settings = options.Value;

		client = new ServiceBusClient(settings.Namespace, new DefaultAzureCredential());
		processor = client.CreateProcessor(settings.Topic, settings.Subscription, new ServiceBusProcessorOptions
		{
			AutoCompleteMessages = false,
			MaxConcurrentCalls = 1
		});

		processor.ProcessMessageAsync += HandleMessageAsync;
		processor.ProcessErrorAsync += HandleErrorAsync;

		await processor.StartProcessingAsync(stoppingToken);

		logger.LogInformation(
			"Listening on service bus topic {Topic}/{Subscription}.",
			settings.Topic,
			settings.Subscription);
	}

	private async Task HandleMessageAsync(ProcessMessageEventArgs args)
	{
		var post = JsonSerializer.Deserialize<Post>(
			args.Message.Body.ToString(),
			JsonSerializerOptions.Web);

		if (post is null)
		{
			logger.LogWarning("Message {MessageId} is not a post, dead lettering it.", args.Message.MessageId);
			await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "The body is not a post.");
			return;
		}

		using var scope = services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<MonkeyBookDbContext>();

		await PostStore.StoreAsync(post, dbContext, logger, args.CancellationToken);
		await args.CompleteMessageAsync(args.Message, args.CancellationToken);
	}

	private Task HandleErrorAsync(ProcessErrorEventArgs args)
	{
		logger.LogError(args.Exception, "Service bus error while {Operation}.", args.ErrorSource);

		return Task.CompletedTask;
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		if (processor is not null)
		{
			await processor.StopProcessingAsync(cancellationToken);
			await processor.DisposeAsync();
		}

		if (client is not null)
		{
			await client.DisposeAsync();
		}

		await base.StopAsync(cancellationToken);
	}
}
