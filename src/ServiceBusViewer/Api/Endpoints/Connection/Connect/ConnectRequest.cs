namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using System.Collections.Generic;
using ServiceBusViewer.Api.Endpoints;

/// <summary>
/// Request to establish a connection to a Service Bus namespace or entity.
/// </summary>
/// <param name="ConnectionString">Primary connection string.</param>
/// <param name="RootConnectionString">Optional root connection string.</param>
/// <param name="QueueOrTopicName">Optional queue or topic name to connect to.</param>
/// <param name="SubscriptionName">Optional subscription name when connecting to a subscription.</param>
internal sealed record ConnectRequest(
	string? ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);

internal static class ConnectRequestValidator
{
	public static void Validate(ConnectRequest request, IDictionary<string, string[]> errors)
	{
		RequestValidation.Require(request.ConnectionString, nameof(request.ConnectionString), "Connection string is required.", errors);

		string? rootConnectionString = RequestValidation.NormalizeOptional(request.RootConnectionString);
		string? queueOrTopicName = RequestValidation.NormalizeOptional(request.QueueOrTopicName);

		if (rootConnectionString is null && queueOrTopicName is null)
			errors[nameof(request.QueueOrTopicName)] = ["Queue/Topic name is required when not using root connection."];
	}
}
