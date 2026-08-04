namespace ServiceBusViewer.Business.Contracts.Viewer;

using ServiceBusViewer.Business.Contracts.ServiceBus;

internal sealed record SendCommand(
	string Body,
	MessageProperties MessageProperties,
	IReadOnlyList<ApplicationProperty> ApplicationProperties);
