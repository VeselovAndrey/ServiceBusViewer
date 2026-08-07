namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents a SQL filter rule for a Service Bus subscription.</summary>
/// <param name="Name">The rule name.</param>
/// <param name="SqlExpression">The SQL expression.</param>
/// <param name="ActionExpression">Optional action expression.</param>
public sealed record SqlSubscriptionFilterRule(
	string Name,
	string SqlExpression,
	string? ActionExpression) : SubscriptionFilterRule(Name);
