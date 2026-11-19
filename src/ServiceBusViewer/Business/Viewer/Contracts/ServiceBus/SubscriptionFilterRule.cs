namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Base type for Service Bus subscription filter rules.</summary>
/// <param name="Name">The rule name.</param>
public closed record SubscriptionFilterRule(string Name);
