namespace ServiceBusViewer.Api.Endpoints.Viewer.UpdatePeekMessageCount;

using System.Collections.Generic;
using ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>
/// Request to update the number of messages peeked for the viewer message list in the current session.
/// </summary>
/// <param name="PeekMessageCount">Number of messages to peek. Must be a whole number from 1 through 500.</param>
internal sealed record UpdatePeekMessageCountRequest(int PeekMessageCount);

/// <summary>Validator for <see cref="UpdatePeekMessageCountRequest"/>.</summary>
internal static class UpdatePeekMessageCountRequestValidator
{
	/// <summary>Validates an <see cref="UpdatePeekMessageCountRequest"/> instance.</summary>
	/// <param name="request">The <see cref="UpdatePeekMessageCountRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary to receive validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(UpdatePeekMessageCountRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		if (request.PeekMessageCount is < PeekMessageCountBounds.Min or > PeekMessageCountBounds.Max) {
			errors = new Dictionary<string, string[]>(capacity: 1) {
				[nameof(request.PeekMessageCount)] = [$"Peek message count must be a whole number from {PeekMessageCountBounds.Min} through {PeekMessageCountBounds.Max}."]
			};
			return false;
		}

		errors = null;
		return true;
	}
}
