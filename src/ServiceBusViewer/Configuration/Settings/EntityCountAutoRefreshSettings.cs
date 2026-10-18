namespace ServiceBusViewer.Configuration.Settings;

using Microsoft.Extensions.Options;
using ServiceBusViewer.Business.Application.Dependencies;

/// <summary>Settings for automatic entity message-count refresh, bound from the ServiceBusViewer:EntityCountAutoRefresh configuration section.</summary>
internal sealed class EntityCountAutoRefreshSettings(IOptions<EntityCountAutoRefreshOptions> options) : IEntityCountAutoRefreshSettings
{
	private readonly IOptions<EntityCountAutoRefreshOptions> _options = options;

	/// <inheritdoc/>
	public bool EntityCountAutoRefreshEnabled => _options.Value.Enabled ?? false;
}

/// <summary>Configuration section backing automatic entity message-count refresh: ServiceBusViewer:EntityCountAutoRefresh.</summary>
internal sealed class EntityCountAutoRefreshOptions
{
	/// <summary>Whether automatic entity message-count refresh is offered to the user. Defaults to disabled.</summary>
	public bool? Enabled { get; init; }
}