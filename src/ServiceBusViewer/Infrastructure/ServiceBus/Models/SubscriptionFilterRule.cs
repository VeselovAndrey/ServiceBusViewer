namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Base class for Service Bus subscription filter rules.</summary>
public abstract record SubscriptionFilterRule(string Name);

/// <summary>SQL filter rule that evaluates expressions against message properties.</summary>
/// <param name="Name">Rule name.</param>
/// <param name="SqlExpression">SQL filter expression.</param>
/// <param name="ActionExpression">Optional SQL action expression.</param>
public sealed record SqlSubscriptionFilterRule(
	string Name,
	string SqlExpression,
	string? ActionExpression) : SubscriptionFilterRule(Name);

/// <summary>Correlation filter rule that matches specific message properties.</summary>
/// <param name="Name">Rule name.</param>
/// <param name="CorrelationId">Correlation ID filter.</param>
/// <param name="MessageId">Message ID filter.</param>
/// <param name="To">To property filter.</param>
/// <param name="ReplyTo">Reply-to property filter.</param>
/// <param name="Subject">Subject property filter.</param>
/// <param name="SessionId">Session ID filter.</param>
/// <param name="ReplyToSessionId">Reply-to session ID filter.</param>
/// <param name="ContentType">Content type filter.</param>
/// <param name="ApplicationProperties">Custom application properties to filter on.</param>
/// <param name="ActionExpression">Optional SQL action expression.</param>
public sealed record CorrelationSubscriptionFilterRule(
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
	string? ActionExpression) : SubscriptionFilterRule(Name);

/// <summary>Unknown or unsupported rule type.</summary>
/// <param name="Name">Rule name.</param>
/// <param name="FilterTypeName">Name of the unsupported filter type.</param>
/// <param name="FilterExpression">Filter details or error message.</param>
public sealed record UnknownSubscriptionFilterRule(
	string Name,
	string FilterTypeName,
	string FilterExpression) : SubscriptionFilterRule(Name);
