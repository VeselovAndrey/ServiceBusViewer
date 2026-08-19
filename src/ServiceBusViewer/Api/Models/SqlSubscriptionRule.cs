namespace ServiceBusViewer.Api.Models;

/// <summary>Describes a SQL subscription rule.</summary>
/// <param name="Name">Rule name.</param>
/// <param name="SqlExpression">SQL filter expression.</param>
/// <param name="ActionExpression">Optional action expression applied by the rule.</param>
public sealed record SqlSubscriptionRule(
	string Name,
	string SqlExpression,
	string? ActionExpression) : SubscriptionRule(Name);
