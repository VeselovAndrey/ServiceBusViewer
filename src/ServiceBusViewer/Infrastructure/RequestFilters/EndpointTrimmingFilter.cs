namespace ServiceBusViewer.Infrastructure.RequestFilters;

public class EndpointTrimmingFilter : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		for (int i = 0; i < context.Arguments.Count; i++) {
			if (context.Arguments[i] is string stringValue)
				context.Arguments[i] = stringValue.Trim();
		}

		return await next(context);
	}
}