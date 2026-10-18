namespace ServiceBusViewer.Business.Application.Dependencies;

/// <summary>Provides configuration for automatic entity message-count refresh.</summary>
public interface IEntityCountAutoRefreshSettings
{
	/// <summary>Gets whether automatic entity message-count refresh is offered to the user.</summary>
	bool EntityCountAutoRefreshEnabled { get; }
}
