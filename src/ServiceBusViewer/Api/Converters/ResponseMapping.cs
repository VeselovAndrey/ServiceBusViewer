namespace ServiceBusViewer.Api.Converters;

using System.Globalization;

/// <summary>Provides value normalization helpers for API responses.</summary>
internal static class ResponseMapping
{
	/// <summary>Converts supported object values to JSON-friendly response representations.</summary>
	/// <param name="value">The value to normalize.</param>
	/// <returns>The normalized value, or the original value when no conversion is required.</returns>
	internal static object? NormalizeObjectValue(object? value)
		=> value switch {
			DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
			DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
			TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
			Guid guid => guid.ToString(),
			char character => character.ToString(),
			byte[] bytes => Convert.ToBase64String(bytes),
			_ => value
		};
}