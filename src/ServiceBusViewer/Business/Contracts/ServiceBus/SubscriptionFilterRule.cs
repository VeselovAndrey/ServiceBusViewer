namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Base type for Service Bus subscription filter rules.</summary>
/// <param name="Name">The rule name.</param>
internal abstract record SubscriptionFilterRule(string Name);
