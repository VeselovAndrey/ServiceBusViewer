namespace ServiceBusViewer.Api.Models;

/// <summary>Describes a correlation subscription rule.</summary>
/// <param name="Name">Rule name.</param>
/// <param name="CorrelationId">Optional correlation id filter value.</param>
/// <param name="MessageId">Optional message id filter value.</param>
/// <param name="To">Optional to-address filter value.</param>
/// <param name="ReplyTo">Optional reply-to filter value.</param>
/// <param name="Subject">Optional subject filter value.</param>
/// <param name="SessionId">Optional session id filter value.</param>
/// <param name="ReplyToSessionId">Optional reply-to session id filter value.</param>
/// <param name="ContentType">Optional content type filter value.</param>
/// <param name="ApplicationProperties">Application properties used by the correlation filter.</param>
/// <param name="ActionExpression">Optional action expression applied by the rule.</param>
public sealed record CorrelationSubscriptionRule(
	string Name,
	string? CorrelationId,
	string? MessageId,
	string? To,
	string? ReplyTo,
	string? Subject,
	string? SessionId,
	string? ReplyToSessionId,
	string? ContentType,
	IReadOnlyDictionary<string, object?> ApplicationProperties,
	string? ActionExpression) : SubscriptionRule(Name);
