namespace ServiceBusViewer.Api.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceBusViewer.Api.Models;

/// <summary>Writes kind-discriminated subscription rule responses.</summary>
internal sealed class SubscriptionRuleJsonConverter : JsonConverter<SubscriptionRule>
{
	/// <inheritdoc />
	public override SubscriptionRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> throw new NotSupportedException("Subscription rules are response-only models.");

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, SubscriptionRule value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		switch (value) {
			case SqlSubscriptionRule sqlRule:
				WriteSqlRule(writer, sqlRule);
				break;

			case CorrelationSubscriptionRule correlationRule:
				WriteCorrelationRule(writer, correlationRule, options);
				break;

			case UnknownSubscriptionRule unknownRule:
				WriteUnknownRule(writer, unknownRule);
				break;

			default:
				throw new JsonException($"Unknown subscription rule type: {value.GetType().FullName}");
		}

		writer.WriteEndObject();
	}

	private static void WriteSqlRule(Utf8JsonWriter writer, SqlSubscriptionRule rule)
	{
		writer.WriteString("kind", "Sql");
		writer.WriteString("name", rule.Name);
		writer.WriteString("sqlExpression", rule.SqlExpression);
		writer.WriteString("actionExpression", rule.ActionExpression);
	}

	private static void WriteCorrelationRule(
		Utf8JsonWriter writer,
		CorrelationSubscriptionRule rule,
		JsonSerializerOptions options)
	{
		writer.WriteString("kind", "Correlation");
		writer.WriteString("name", rule.Name);
		writer.WriteString("correlationId", rule.CorrelationId);
		writer.WriteString("messageId", rule.MessageId);
		writer.WriteString("to", rule.To);
		writer.WriteString("replyTo", rule.ReplyTo);
		writer.WriteString("subject", rule.Subject);
		writer.WriteString("sessionId", rule.SessionId);
		writer.WriteString("replyToSessionId", rule.ReplyToSessionId);
		writer.WriteString("contentType", rule.ContentType);
		writer.WritePropertyName("applicationProperties");
		JsonSerializer.Serialize(writer, rule.ApplicationProperties, options);
		writer.WriteString("actionExpression", rule.ActionExpression);
	}

	private static void WriteUnknownRule(Utf8JsonWriter writer, UnknownSubscriptionRule rule)
	{
		writer.WriteString("kind", "Unknown");
		writer.WriteString("name", rule.Name);
		writer.WriteString("filterTypeName", rule.FilterTypeName);
		writer.WriteString("filterExpression", rule.FilterExpression);
	}
}
