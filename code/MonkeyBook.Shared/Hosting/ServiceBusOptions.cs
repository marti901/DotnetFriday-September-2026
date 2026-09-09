namespace MonkeyBook.Shared;

// Bound to the ServiceBus section, it is only filled in azure.
// When the namespace is empty the services fall back to the dapr pub/sub used by aspire locally.
public sealed class ServiceBusOptions
{
	public const string SectionName = "ServiceBus";

	public string? Namespace { get; set; }

	public string Topic { get; set; } = "post-created";

	public string Subscription { get; set; } = "background-processor";

	public bool IsConfigured => !string.IsNullOrWhiteSpace(Namespace);
}
