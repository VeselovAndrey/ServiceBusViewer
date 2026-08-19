namespace ServiceBusViewer.Api.Endpoints.Viewer.Receive;

using System.Collections.Generic;

/// <summary>
/// Request to receive messages; optionally specifies a session id to receive from.
/// </summary>
/// <param name="ReceiveSessionId">Optional session id to receive from.</param>
public sealed record ReceiveRequest(string? ReceiveSessionId);

internal static class ReceiveRequestValidator
{
	public static bool Validate(ReceiveRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		// No specific validations for ReceiveRequest at this time.
		errors = null;
		return true;
	}
}
