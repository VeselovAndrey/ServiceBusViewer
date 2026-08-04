namespace ServiceBusViewer.Business.Contracts.ServiceBus;

internal sealed record UnknownSubscriptionFilterRule(
	string Name,
	string FilterTypeName,
	string FilterExpression) : SubscriptionFilterRule(Name);
