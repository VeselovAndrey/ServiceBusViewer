namespace ServiceBusViewer.Api.ExceptionHandling;

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
		(int status, string title, string detail, bool isUnexpected) = exception switch {
			ViewerAlreadyConnectedException => (StatusCodes.Status409Conflict, "Already connected", exception.Message, false),
			ViewerNotConnectedException => (StatusCodes.Status409Conflict, "Not connected", exception.Message, false),
			ArgumentException or FormatException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message, false),
			ServiceBusException => (StatusCodes.Status400BadRequest, "Service Bus request failed", exception.Message, false),
			_ => (StatusCodes.Status500InternalServerError, "Request failed", "An unexpected error occurred while processing the request.", true)
		};

		if (isUnexpected)
			_logger.LogError(exception, "An unexpected exception occurred while processing an API request.");

		httpContext.Response.StatusCode = status;

		return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext {
			HttpContext = httpContext,
			ProblemDetails = new ProblemDetails {
				Status = status,
				Title = title,
				Detail = detail
			}
		});
	}
}
