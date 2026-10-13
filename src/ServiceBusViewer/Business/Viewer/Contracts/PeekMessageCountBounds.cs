namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Defines the allowed bounds for the viewer peek message count.</summary>
public static class PeekMessageCountBounds
{
	/// <summary>Minimum number of messages that can be peeked for the viewer message list.</summary>
	public const int Min = 1;

	/// <summary>Maximum number of messages that can be peeked for the viewer message list.</summary>
	public const int Max = 500;
}
