namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Provides entity selection and entity details operations for a viewer session.</summary>
public interface IViewerEntityService
{
	/// <summary>Selects the active entity for subsequent viewer operations.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="entityId">The entity to select.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The resulting viewer state after selection.</returns>
	Task<ViewerState> SelectEntityAsync(IViewerSessionState session, EntityId entityId, CancellationToken cancellationToken);

	/// <summary>Gets detailed metadata for a specific entity.</summary>
	/// <param name="session">The viewer session state used for the lookup.</param>
	/// <param name="entityId">The entity to describe.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>Detailed information about the requested entity.</returns>
	Task<EntityDetailsResult> GetDetailsAsync(IViewerSessionState session, EntityId entityId, CancellationToken cancellationToken);
}
