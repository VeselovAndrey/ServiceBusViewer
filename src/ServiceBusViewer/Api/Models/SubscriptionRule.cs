namespace ServiceBusViewer.Api.Models;

/// <summary>Base type for subscription rule responses.</summary>
/// <param name="Name">Rule name.</param>
internal abstract record SubscriptionRule(string Name);
