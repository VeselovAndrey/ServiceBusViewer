namespace ServiceBusViewer.Api.ExceptionHandling;

using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Converts unhandled API exceptions into consistent RFC problem-details responses.</summary>
/// <param name="problemDetailsService">The service used to write problem-details responses.</param>
/// <param name="logger">The logger used to record unexpected exceptions.</param>
internal sealed class ApiExceptionHandler(
	IProblemDetailsService problemDetailsService,
	ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
	private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;
	private readonly ILogger<ApiExceptionHandler> _logger = logger;

	/// <summary>Writes a classified problem-details response for an unhandled API exception.</summary>
	/// <param name="httpContext">The HTTP context for the failed request.</param>
	/// <param name="exception">The unhandled exception to classify.</param>
	/// <param name="cancellationToken">The token that signals request cancellation.</param>
	/// <returns><see langword="true"/> when the exception response was written; otherwise, <see langword="false"/>.</returns>
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
			return false;

		ExceptionResponse response = ClassifyException(exception);

		if (response.IsUnexpected)
			_logger.LogError(exception, "An unexpected exception occurred while processing an API request.");

		httpContext.Response.StatusCode = response.Status;

		return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext {
			HttpContext = httpContext,
			ProblemDetails = new ProblemDetails {
				Status = response.Status,
				Title = response.Title,
				Detail = response.Detail
			}
		});
	}

	private static ExceptionResponse ClassifyException(Exception exception)
	{
		ExceptionResponse? response = TryClassifyKnownException(exception);
		if (response is not null)
			return response.Value;

		if (exception is AggregateException aggregateException) {
			foreach (Exception innerException in aggregateException.Flatten().InnerExceptions) {
				response = TryClassifyKnownException(innerException);
				if (response is not null)
					return response.Value;
			}
		}

		return new ExceptionResponse(
			StatusCodes.Status500InternalServerError,
			"Request failed",
			"An unexpected error occurred while processing the request.",
			true);
	}

	private static ExceptionResponse? TryClassifyKnownException(Exception exception)
		=> exception switch {
			ViewerAlreadyConnectedException => new ExceptionResponse(StatusCodes.Status409Conflict, "Already connected", exception.Message, false),
			ViewerNotConnectedException => new ExceptionResponse(StatusCodes.Status409Conflict, "Not connected", exception.Message, false),
			SendMessageValidationException => new ExceptionResponse(StatusCodes.Status400BadRequest, "Invalid request", exception.Message, false),
			OperationCanceledException => new ExceptionResponse(StatusCodes.Status409Conflict, "Operation cancelled", "The Service Bus operation was cancelled because the viewer disconnected.", false),
			ArgumentException or FormatException => new ExceptionResponse(StatusCodes.Status400BadRequest, "Invalid request", exception.Message, false),
			RequestFailedException or ServiceBusException => new ExceptionResponse(
				StatusCodes.Status400BadRequest,
				"Service Bus request failed",
				exception.Message,
				false),
			_ => null
		};

	private readonly record struct ExceptionResponse(int Status, string Title, string Detail, bool IsUnexpected);
}
