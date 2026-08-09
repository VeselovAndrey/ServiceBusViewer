namespace ServiceBusViewer.Api.Converters;

/// <summary>Provides value normalization helpers for API requests.</summary>
internal static class RequestMapping
{

	/// <summary>Trims an optional string and converts null, empty, or whitespace-only values to null.</summary>
	/// <param name="value">The optional string to normalize.</param>
	/// <returns>The trimmed value, or <see langword="null"/> when the value has no non-whitespace content.</returns>
	public static string? NormalizeOptional(string? value)
		=> string.IsNullOrWhiteSpace(value)
			? null
			: value.Trim();
}