namespace ServiceBusViewer.Business.Viewer.Dependencies;

using ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Creates open Service Bus connections from viewer connection settings.</summary>
public interface IServiceBusConnectionFactory
{
	/// <summary>Opens a connection for the given viewer connection settings.</summary>
	/// <param name="settings">Viewer-provided connection settings describing the Service Bus namespace and credentials.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes with an <see cref="IServiceBusConnection"/> representing the opened connection.</returns>
	Task<IServiceBusConnection> OpenAsync(ConnectionSettings settings, CancellationToken cancellationToken);
}
