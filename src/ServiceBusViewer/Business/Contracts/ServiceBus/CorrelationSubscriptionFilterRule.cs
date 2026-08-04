namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Represents a correlation filter rule for a Service Bus subscription.</summary>
/// <param name="Name">The rule name.</param>
/// <param name="CorrelationId">Optional correlation identifier filter.</param>
/// <param name="MessageId">Optional message identifier filter.</param>
/// <param name="To">Optional To filter.</param>
/// <param name="ReplyTo">Optional ReplyTo filter.</param>
/// <param name="Subject">Optional subject filter.</param>
/// <param name="SessionId">Optional session identifier filter.</param>
/// <param name="ReplyToSessionId">Optional ReplyToSessionId filter.</param>
/// <param name="ContentType">Optional content-type filter.</param>
/// <param name="ApplicationProperties">Optional application property filters.</param>
/// <param name="ActionExpression">Optional action expression.</param>
internal sealed record CorrelationSubscriptionFilterRule(
	string Name,
	string? CorrelationId,
	string? MessageId,
	string? To,
	string? ReplyTo,
	string? Subject,
	string? SessionId,
	string? ReplyToSessionId,
	string? ContentType,
	IReadOnlyDictionary<string, object> ApplicationProperties,
	string? ActionExpression)
	: SubscriptionFilterRule(Name);
