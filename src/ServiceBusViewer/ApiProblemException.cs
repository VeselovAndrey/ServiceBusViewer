namespace ServiceBusViewer;

internal sealed class ApiProblemException(int statusCode, string title, string detail) : Exception(detail)
{
	public int StatusCode { get; } = statusCode;

	public string Title { get; } = title;

	public IDictionary<string, string[]>? Errors { get; init; }

	public static ApiProblemException Validation(IDictionary<string, string[]> errors)
	{
		return new ApiProblemException(StatusCodes.Status400BadRequest, "Validation failed", "One or more validation errors occurred.") {
			Errors = errors
		};
	}
}
