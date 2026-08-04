namespace ServiceBusViewer.Business.Contracts;

using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

/// <summary>Defines the session-backed business operations exposed to the API endpoints.</summary>
internal interface IViewerBusinessService
{
	Task<BootstrapResult> GetBootstrapAsync(BrowserSessionState state);

	Task<ViewerState> ConnectAsync(BrowserSessionState state, ConnectCommand command);

	Task<BootstrapResult> DisconnectAsync(BrowserSessionState state);

	Task<ViewerState> GetViewerAsync(BrowserSessionState state);

	Task<ViewerState> RefreshAsync(BrowserSessionState state);

	Task<ViewerState> SelectEntityAsync(BrowserSessionState state, SelectEntityCommand command);

	Task<ViewerState> ReceiveAsync(BrowserSessionState state, ReceiveCommand command);

	Task<ViewerState> SendAsync(BrowserSessionState state, SendCommand command);

	Task<EntityDetailsResult> GetEntityDetailsAsync(BrowserSessionState state, EntityDetailsQuery query);
}
