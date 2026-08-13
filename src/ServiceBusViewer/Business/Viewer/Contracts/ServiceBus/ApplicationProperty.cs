namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

using System.Diagnostics.CodeAnalysis;

/// <summary>Supported application property value types for outgoing Service Bus messages.</summary>
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Member names are serialized application-property type values shared with the frontend.")]
public enum ApplicationPropertyType
{
	String = 1,
	Bool,
	Byte,
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
	TimeSpan
}

/// <summary>Represents a single application property for an outgoing Service Bus message.</summary>
/// <param name="Key">The property key.</param>
/// <param name="Value">The property value.</param>
/// <param name="Type">The value type.</param>
public sealed record ApplicationProperty(string Key, string Value, ApplicationPropertyType Type = ApplicationPropertyType.String);
