namespace ServiceBusViewer.Api.Endpoints.Viewer.Receive;

using System.Collections.Generic;
using ServiceBusViewer.Api.Endpoints;

/// <summary>
/// Request to receive messages; optionally specifies a session id to receive from.
/// </summary>
/// <param name="ReceiveSessionId">Optional session id to receive from.</param>
internal sealed record ReceiveRequest(string? ReceiveSessionId);

internal static class ReceiveRequestValidator
{
	public static void Validate(ReceiveRequest request, IDictionary<string, string[]> errors)
	{
		// No specific validations for ReceiveRequest at this time.
	}
}
