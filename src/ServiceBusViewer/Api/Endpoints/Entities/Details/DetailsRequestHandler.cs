namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using System.Globalization;
using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class DetailsRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, [AsParameters] DetailsRequest request, IViewerEntityService service, CancellationToken cancellationToken)
	{
		Dictionary<string, string[]> errors = [];

		if (!DetailsRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? detailsErrors) && detailsErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in detailsErrors)
				errors[kv.Key] = kv.Value;
		}

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		Business.Viewer.Contracts.ServiceBus.EntityId entityId = EntityIdFactory.Create(request.Type, request.Name, request.TopicName);

		EntityDetailsResult result = await service.GetDetailsAsync(
			context.GetClientSessionState(),
			entityId,
			cancellationToken);

		return TypedResults.Ok(ToDetailsResponse(result));
	}

	private static DetailsResponse ToDetailsResponse(EntityDetailsResult result)
	{
		return new DetailsResponse(
			result.ServiceBusHostName,
			result.IsManagementApiAvailable,
			[.. result.AvailableEntities.Select(e => e.ToApiModel())],
			result.Type,
			result.Name,
			result.TopicName,
			result.Properties is null ? null : ToEntityPropertiesResponse(result.Properties));
	}

	private static Models.EntityProperties ToEntityPropertiesResponse(Business.Viewer.Contracts.ServiceBus.EntityProperties properties) => properties switch {
		Business.Viewer.Contracts.ServiceBus.QueueEntityProperties queue => new Models.EntityProperties(
			"Queue",
			queue.Name,
			null,
			queue.LockDuration.ToString("c", CultureInfo.InvariantCulture),
			queue.MaxDeliveryCount,
			queue.DefaultMessageTimeToLive.ToString("c", CultureInfo.InvariantCulture),
			queue.RequiresDuplicateDetection,
			queue.DuplicateDetectionHistoryTimeWindow.ToString("c", CultureInfo.InvariantCulture),
			queue.DeadLetteringOnMessageExpiration,
			queue.EnableBatchedOperations,
			queue.RequiresSession,
			queue.EnablePartitioning,
			queue.AutoDeleteOnIdle.ToString("c", CultureInfo.InvariantCulture),
			null),
		Business.Viewer.Contracts.ServiceBus.TopicEntityProperties topic => new Models.EntityProperties(
			"Topic",
			topic.Name,
			null,
			null,
			null,
			topic.DefaultMessageTimeToLive.ToString("c", CultureInfo.InvariantCulture),
			topic.RequiresDuplicateDetection,
			topic.DuplicateDetectionHistoryTimeWindow.ToString("c", CultureInfo.InvariantCulture),
			null,
			topic.EnableBatchedOperations,
			null,
			topic.EnablePartitioning,
			topic.AutoDeleteOnIdle.ToString("c", CultureInfo.InvariantCulture),
			null),
		Business.Viewer.Contracts.ServiceBus.SubscriptionEntityProperties subscription => new Models.EntityProperties(
			"Subscription",
			subscription.Name,
			subscription.TopicName,
			subscription.LockDuration.ToString("c", CultureInfo.InvariantCulture),
			subscription.MaxDeliveryCount,
			subscription.DefaultMessageTimeToLive.ToString("c", CultureInfo.InvariantCulture),
			null,
			null,
			subscription.DeadLetteringOnMessageExpiration,
			subscription.EnableBatchedOperations,
			subscription.RequiresSession,
			null,
			subscription.AutoDeleteOnIdle.ToString("c", CultureInfo.InvariantCulture),
			[.. subscription.Rules.Select(ToSubscriptionRuleResponse)]),
		_ => throw new ArgumentException($"Unknown entity type: {properties.GetType().FullName}", nameof(properties))
	};

	private static SubscriptionRule ToSubscriptionRuleResponse(Business.Viewer.Contracts.ServiceBus.SubscriptionFilterRule rule) => rule switch {
		Business.Viewer.Contracts.ServiceBus.SqlSubscriptionFilterRule sqlRule => new SqlSubscriptionRule(
			sqlRule.Name,
			sqlRule.SqlExpression,
			sqlRule.ActionExpression),

		Business.Viewer.Contracts.ServiceBus.CorrelationSubscriptionFilterRule correlationRule => new CorrelationSubscriptionRule(
			correlationRule.Name,
			correlationRule.CorrelationId,
			correlationRule.MessageId,
			correlationRule.To,
			correlationRule.ReplyTo,
			correlationRule.Subject,
			correlationRule.SessionId,
			correlationRule.ReplyToSessionId,
			correlationRule.ContentType,
			correlationRule.ApplicationProperties.ToDictionary(static pair => pair.Key, static pair => ResponseMapping.NormalizeObjectValue(pair.Value)),
			correlationRule.ActionExpression),

		Business.Viewer.Contracts.ServiceBus.UnknownSubscriptionFilterRule unknownRule => new UnknownSubscriptionRule(
			unknownRule.Name,
			unknownRule.FilterTypeName,
			unknownRule.FilterExpression),
		_ => throw new ArgumentException($"Unknown subscription rule type: {rule.GetType().FullName}", nameof(rule))
	};
}
