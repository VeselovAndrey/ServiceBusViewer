namespace ServiceBusViewer.Pages.Shared;

public sealed record HeaderBarModel(
	HeaderBarStatusModel Status,
	HeaderBarActionModel? Action = null);

public sealed record HeaderBarStatusModel(
	string IndicatorCssClass,
	string Text,
	string? Value = null);

public sealed record HeaderBarActionModel(
	HeaderBarActionKind Kind,
	string Label,
	string Icon,
	string? Page = null,
	string? PageHandler = null,
	string? FormId = null,
	string? SubmitButtonId = null);

public enum HeaderBarActionKind
{
	Link,
	Post
}
