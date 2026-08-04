namespace ServiceBusViewer.Api.Models;

/// <summary>
/// Describes a subscription rule (filter and optional action).
/// </summary>
/// <param name="Kind">Rule kind, e.g., "Sql" or "Correlation".</param>
/// <param name="Name">Rule name.</param>
/// <param name="SqlExpression">SQL filter expression for sql rules.</param>
/// <param name="ActionExpression">Action expression applied by the rule.</param>
/// <param name="CorrelationId">Correlation id filter value.</param>
/// <param name="MessageId">Message id filter value.</param>
/// <param name="To">To-address filter value.</param>
/// <param name="ReplyTo">ReplyTo filter value.</param>
/// <param name="Subject">Subject filter value.</param>
/// <param name="SessionId">SessionId filter value.</param>
/// <param name="ReplyToSessionId">ReplyToSessionId filter value.</param>
/// <param name="ContentType">Content type filter value.</param>
/// <param name="ApplicationProperties">Application properties used in the correlation filter.</param>
/// <param name="FilterTypeName">Unknown rule filter type name.</param>
/// <param name="FilterExpression">Unknown rule filter expression.</param>
internal sealed record SubscriptionRule(
	string Kind,
	string Name,
	string? SqlExpression,
	string? ActionExpression,
	string? CorrelationId,
	string? MessageId,
	string? To,
	string? ReplyTo,
	string? Subject,
	string? SessionId,
	string? ReplyToSessionId,
	string? ContentType,
	IReadOnlyDictionary<string, object?>? ApplicationProperties,
	string? FilterTypeName,
	string? FilterExpression);
