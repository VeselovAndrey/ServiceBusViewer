namespace ServiceBusViewer.Api.Endpoints;

using System.Globalization;

internal static class ResponseMapping
{
	internal static object? NormalizeObjectValue(object? value) => value switch {
		DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
		DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
		TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
		Guid guid => guid.ToString(),
		char character => character.ToString(),
		byte[] bytes => Convert.ToBase64String(bytes),
		_ => value
	};

}
