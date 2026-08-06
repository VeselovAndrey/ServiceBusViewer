namespace ServiceBusViewer.Business.Contracts;

using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.ClientSession;

/// <summary>Defines the session-backed business operations exposed to the API endpoints.</summary>
internal interface IViewerBusinessService
{
	Task<BootstrapResult> GetBootstrapAsync(ClientSessionState state);

	Task<ViewerState> ConnectAsync(ClientSessionState state, ConnectCommand command);

	Task<BootstrapResult> DisconnectAsync(ClientSessionState state);

	Task<ViewerState> GetViewerAsync(ClientSessionState state);

	Task<ViewerState> RefreshAsync(ClientSessionState state);

	Task<ViewerState> SelectEntityAsync(ClientSessionState state, SelectEntityCommand command);

	Task<ViewerState> ReceiveAsync(ClientSessionState state, ReceiveCommand command);

	Task<ViewerState> SendAsync(ClientSessionState state, SendCommand command);

	Task<EntityDetailsResult> GetEntityDetailsAsync(ClientSessionState state, EntityDetailsQuery query);
}
