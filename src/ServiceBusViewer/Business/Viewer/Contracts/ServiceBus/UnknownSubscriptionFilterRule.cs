namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents a subscription rule whose filter type is not modeled explicitly.</summary>
/// <param name="Name">The rule name.</param>
/// <param name="FilterTypeName">The runtime filter type name.</param>
/// <param name="FilterExpression">A textual description of the filter.</param>
public sealed record UnknownSubscriptionFilterRule(
	string Name,
	string FilterTypeName,
	string FilterExpression) : SubscriptionFilterRule(Name);
