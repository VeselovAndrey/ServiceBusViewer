namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Represents a user-provided application property for a Service Bus message.</summary>
public sealed record ApplicationProperty(string Key, string Value, ApplicationPropertyType Type = ApplicationPropertyType.String);
