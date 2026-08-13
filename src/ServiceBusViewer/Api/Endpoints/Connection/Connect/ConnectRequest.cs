namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using System.Collections.Generic;

/// <summary>
/// Request to establish a connection to a Service Bus namespace or entity.
/// </summary>
/// <param name="ConnectionString">Primary connection string.</param>
/// <param name="EmulatorManagementConnectionString">Optional emulator-only management connection string.</param>
/// <param name="QueueOrTopicName">
/// Optional queue or topic name. Without management access, this identifies a queue unless
/// <paramref name="SubscriptionName"/> is also provided, in which case it identifies the parent topic.
/// </param>
/// <param name="SubscriptionName">Optional subscription name for direct access through its parent topic.</param>
internal sealed record ConnectRequest(
	string ConnectionString,
	string? EmulatorManagementConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);

internal static class ConnectRequestValidator
{
	public static bool Validate(ConnectRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		var local = new Dictionary<string, string[]>(capacity: 1);
		if (string.IsNullOrWhiteSpace(request.ConnectionString))
			local[nameof(request.ConnectionString)] = ["Connection string is required."];

		if (local.Count > 0) {
			errors = local;
			return false;
		}

		errors = null;
		return true;
	}
}
