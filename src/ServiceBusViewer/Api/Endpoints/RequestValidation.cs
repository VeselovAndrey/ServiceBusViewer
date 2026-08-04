namespace ServiceBusViewer.Api.Endpoints;

using System.Globalization;

internal static class RequestValidation
{
	private const int _maxSystemPropertyLength = 128;

	public static string? NormalizeOptional(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	public static string? Require(string? value, string fieldName, string message, IDictionary<string, string[]> errors)
	{
		string? normalized = NormalizeOptional(value);
		if (normalized is null)
			errors[fieldName] = [message];

		return normalized;
	}

	public static void ValidateSystemPropertyLength(string? value, string propertyName, string displayName, IDictionary<string, string[]> errors)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Length <= _maxSystemPropertyLength)
			return;

		errors[propertyName] = [$"The {displayName} must be {_maxSystemPropertyLength} characters or fewer."];
	}

	public static DateTimeOffset? ParseDateTimeOffset(string? value, string fieldName, IDictionary<string, string[]> errors)
	{
		if (value is null)
			return null;

		if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset result))
			return result;

		errors[fieldName] = ["Scheduled enqueue time must be a valid date/time value."];
		return null;
	}

	public static TimeSpan? ParseTimeSpan(string? value, string fieldName, IDictionary<string, string[]> errors)
	{
		if (value is null)
			return null;

		if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan result))
			return result;

		errors[fieldName] = ["Time to live must be a valid time span value."];
		return null;
	}

	public static void ThrowIfAny(IDictionary<string, string[]> errors)
	{
		if (errors.Count > 0)
			throw ApiProblemException.Validation(errors);
	}
}
