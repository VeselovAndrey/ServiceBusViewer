namespace ServiceBusViewer.Api.Converters;

using System.Globalization;
using ApiApplicationPropertyType = ServiceBusViewer.Api.Models.ApplicationPropertyType;

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

	/// <summary>Gets the serialized application-property type for a supported value.</summary>
	/// <param name="value">The application-property value.</param>
	/// <returns>The serialized application-property type.</returns>
	internal static ApiApplicationPropertyType GetApplicationPropertyType(object? value)
		=> value switch {
			string => ApiApplicationPropertyType.String,
			bool => ApiApplicationPropertyType.Bool,
			byte => ApiApplicationPropertyType.Byte,
			byte[] => ApiApplicationPropertyType.ByteArray,
			sbyte => ApiApplicationPropertyType.SByte,
			short => ApiApplicationPropertyType.Short,
			ushort => ApiApplicationPropertyType.UShort,
			int => ApiApplicationPropertyType.Int,
			uint => ApiApplicationPropertyType.UInt,
			long => ApiApplicationPropertyType.Long,
			ulong => ApiApplicationPropertyType.ULong,
			float => ApiApplicationPropertyType.Float,
			double => ApiApplicationPropertyType.Double,
			decimal => ApiApplicationPropertyType.Decimal,
			char => ApiApplicationPropertyType.Char,
			Guid => ApiApplicationPropertyType.Guid,
			DateTime => ApiApplicationPropertyType.DateTime,
			DateTimeOffset => ApiApplicationPropertyType.DateTimeOffset,
			TimeSpan => ApiApplicationPropertyType.TimeSpan,
			null => ApiApplicationPropertyType.Null,
			_ => ApiApplicationPropertyType.Unknown
		};
}