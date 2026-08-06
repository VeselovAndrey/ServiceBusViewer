namespace ServiceBusViewer.Api.Endpoints;

using System.Collections.Generic;

internal static class RequestValidation
{
	public static string? NormalizeOptional(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
