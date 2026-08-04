namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Supported application property value types for outgoing Service Bus messages.</summary>
internal enum ApplicationPropertyType
{
	String = 0,
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
internal sealed record ApplicationProperty(string Key, string Value, ApplicationPropertyType Type = ApplicationPropertyType.String);
