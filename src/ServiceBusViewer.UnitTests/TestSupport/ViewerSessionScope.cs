namespace ServiceBusViewer.UnitTests.TestSupport;

using NSubstitute;
using ServiceBusViewer.Business.Viewer.Dependencies;

internal sealed class ViewerSessionScope : IDisposable
{
	private readonly SemaphoreSlim _gate = new(1, 1);

	internal ViewerSessionScope()
	{
		State = Substitute.For<IViewerSessionState>();
		State.Gate.Returns(_gate);
		State.ConnectionCancellationToken.Returns(CancellationToken.None);
	}

	internal IViewerSessionState State { get; }

	internal static ViewerSessionScope CreateDisconnected()
	{
		var scope = new ViewerSessionScope();
		scope.State.Connection.Returns((IServiceBusConnection?)null);
		return scope;
	}

	public void Dispose() => _gate.Dispose();
}
