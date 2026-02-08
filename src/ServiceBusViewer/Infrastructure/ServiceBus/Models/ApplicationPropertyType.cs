namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Supported application property types for Service Bus messages.</summary>
public enum ApplicationPropertyType
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