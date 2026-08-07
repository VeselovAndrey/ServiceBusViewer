namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates connection lifecycle operations for a single browser session.</summary>
internal sealed class ViewerConnectionService(
	IApplicationInfoProvider applicationInfoProvider,
	IServiceBusConnectionFactory connectionFactory) : IViewerConnectionService
{
	public async Task<BootstrapResult> GetBootstrapAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			return ViewerSessionStateMapper.ToBootstrapResult(session, applicationInfoProvider.ApplicationVersion);
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> ConnectAsync(IViewerSessionState session, ConnectionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		await session.Gate.WaitAsync();

		try {
			if (session.IsConnected)
				throw new ApiProblemException(StatusCodes.Status409Conflict, "Already connected", "Already connected to a Service Bus instance.");

			IServiceBusConnection connection = await connectionFactory.OpenAsync(settings);

			try {
				ConnectionSettings normalizedSettings = ViewerSessionStateMapper.NormalizeConnectionSettings(settings);
				session.ConnectionSettings = normalizedSettings;
				session.Connection = connection;
				session.SelectedEntityId = ViewerSessionStateMapper.GetInitialSelectedEntityId(normalizedSettings);
				session.CurrentMessages = session.SelectedEntityId is null
					? ReceivedMessageList.Empty
					: await connection.PeekMessagesAsync(session.SelectedEntityId);
				session.DisplayedMessage = null;
				session.SendResultMessage = null;
				session.ReceiveSessionId = null;

				return ViewerSessionStateMapper.ToViewerState(session, connection);
			}
			catch {
				await connection.DisposeAsync();
				throw;
			}
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<BootstrapResult> DisconnectAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			await session.ResetConnectionAsync();
			return ViewerSessionStateMapper.ToBootstrapResult(session, applicationInfoProvider.ApplicationVersion);
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> GetCurrentAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			return ViewerSessionStateMapper.ToViewerState(session, ViewerSessionStateMapper.EnsureConnected(session));
		}
		finally {
			session.Gate.Release();
		}
	}
}
