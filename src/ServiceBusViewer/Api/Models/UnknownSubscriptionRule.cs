namespace ServiceBusViewer.Api.Models;

/// <summary>Describes a subscription rule whose filter type is not supported explicitly.</summary>
/// <param name="Name">Rule name.</param>
/// <param name="FilterTypeName">Runtime filter type name.</param>
/// <param name="FilterExpression">Textual filter representation.</param>
internal sealed record UnknownSubscriptionRule(
	string Name,
	string FilterTypeName,
	string FilterExpression) : SubscriptionRule(Name);
