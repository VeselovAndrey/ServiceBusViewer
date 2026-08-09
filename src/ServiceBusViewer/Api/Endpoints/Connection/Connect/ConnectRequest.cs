namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using System.Collections.Generic;

/// <summary>
/// Request to establish a connection to a Service Bus namespace or entity.
/// </summary>
/// <param name="ConnectionString">Primary connection string.</param>
/// <param name="RootConnectionString">Optional root connection string.</param>
/// <param name="QueueOrTopicName">Optional queue or topic name to connect to.</param>
/// <param name="SubscriptionName">Optional subscription name when connecting to a subscription.</param>
internal sealed record ConnectRequest(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);

internal static class ConnectRequestValidator
{
	public static bool Validate(ConnectRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.ConnectionString))
			local[nameof(request.ConnectionString)] = ["Connection string is required."];

		if (string.IsNullOrWhiteSpace(request.RootConnectionString) && string.IsNullOrWhiteSpace(request.QueueOrTopicName))
			local[nameof(request.QueueOrTopicName)] = ["Queue/Topic name is required when not using root connection."];

		if (local.Count > 0) {
			errors = local;
			return false;
		}

		errors = null;
		return true;
	}
}
