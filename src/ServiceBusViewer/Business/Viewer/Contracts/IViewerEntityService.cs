namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>
/// Coordinates entity selection and entity details operations for a single browser session.
/// </summary>
public interface IViewerEntityService
{
	Task<ViewerState> SelectEntityAsync(IViewerSessionState session, EntityId entityId);

	Task<EntityDetailsResult> GetDetailsAsync(IViewerSessionState session, EntityId entityId);
}
