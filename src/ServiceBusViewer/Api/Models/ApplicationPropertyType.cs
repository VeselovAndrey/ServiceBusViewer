namespace ServiceBusViewer.Api.Models;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>Serialized application property type values exposed by the API.</summary>
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Member names are serialized application-property type values shared with the frontend.")]
[JsonConverter(typeof(JsonStringEnumConverter<ApplicationPropertyType>))]
public enum ApplicationPropertyType
{
	String = 1,
	Bool,
	Byte,
	ByteArray,
	SByte,
	Short,
	UShort,
	Int,
	UInt,
	Long,
	ULong,
	Float,
	Double,
	Decimal,
	Char,
	Guid,
	DateTime,
	DateTimeOffset,
	TimeSpan,
	Null,
	Unknown
}
