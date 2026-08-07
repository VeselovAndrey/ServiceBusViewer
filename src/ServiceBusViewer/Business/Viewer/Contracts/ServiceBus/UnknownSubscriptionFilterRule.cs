namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

public sealed record UnknownSubscriptionFilterRule(
	string Name,
	string FilterTypeName,
	string FilterExpression) : SubscriptionFilterRule(Name);
