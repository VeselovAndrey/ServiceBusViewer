export type EntityType = 'Queue' | 'Topic' | 'Subscription';

export interface ConnectionSettingsDto {
	connectionString: string;
	rootConnectionString: string | null;
	queueOrTopicName: string | null;
	subscriptionName: string | null;
}

export interface EntityIdDto {
	type: EntityType;
	name: string;
	topicName: string | null;
}

export interface ReceivedMessagePropertiesDto {
	messageId: string;
	partitionKey: string | null;
	sessionId: string | null;
	correlationId: string | null;
	contentType: string | null;
	enqueuedTimeUtc: string;
	scheduledEnqueueTime: string | null;
	timeToLive: string | null;
}

export interface ReceivedMessageDto {
	body: string;
	properties: ReceivedMessagePropertiesDto;
	applicationProperties: Record<string, unknown> | null;
}

export interface ViewerState {
	serviceBusHostName: string;
	entityName: string | null;
	topicName: string | null;
	requiresSession: boolean;
	isManagementApiAvailable: boolean;
	availableEntities: EntityIdDto[];
	messages: ReceivedMessageDto[];
	hasMoreMessages: boolean;
	displayedMessage: ReceivedMessageDto | null;
	sendResultMessage: string | null;
	receiveSessionId: string | null;
}

export interface SessionStateDto {
	applicationVersion: string;
	isRunningInContainer: boolean;
	connection: ConnectionSettingsDto;
	viewer: ViewerState | null;
	isConnected: boolean;
}

export interface ConnectRequestDto {
	connectionString: string;
	rootConnectionString: string | null;
	queueOrTopicName: string | null;
	subscriptionName: string | null;
}

export interface ReceiveRequestDto {
	receiveSessionId: string | null;
}

export type ApplicationPropertyType =
	| 'String'
	| 'Bool'
	| 'Byte'
	| 'SByte'
	| 'Short'
	| 'UShort'
	| 'Int'
	| 'UInt'
	| 'Long'
	| 'ULong'
	| 'Float'
	| 'Double'
	| 'Decimal'
	| 'Char'
	| 'Guid'
	| 'DateTime'
	| 'DateTimeOffset'
	| 'TimeSpan';

export interface ApplicationPropertyInputDto {
	key: string;
	type: ApplicationPropertyType;
	value: string;
}

export interface SendMessagePropertiesDto {
	messageId: string;
	sessionId: string;
	correlationId: string;
	contentType: string;
	scheduledEnqueueTime: string | null;
	timeToLive: string;
}

export interface SendMessageRequestDto {
	sendMessageBody: string;
	sendMessageProperties: SendMessagePropertiesDto;
	sendMessageApplicationProperties: ApplicationPropertyInputDto[];
}

export interface EntityDetailsDto {
	serviceBusHostName: string;
	isManagementApiAvailable: boolean;
	availableEntities: EntityIdDto[];
	type: EntityType;
	name: string;
	topicName: string | null;
	properties: EntityPropertiesDto | null;
}

export interface QueueEntityPropertiesDto {
	name: string;
	lockDuration: string;
	maxDeliveryCount: number;
	defaultMessageTimeToLive: string;
	requiresDuplicateDetection: boolean;
	duplicateDetectionHistoryTimeWindow: string;
	deadLetteringOnMessageExpiration: boolean;
	enableBatchedOperations: boolean;
	requiresSession: boolean;
	enablePartitioning: boolean;
	autoDeleteOnIdle: string;
}

export interface TopicEntityPropertiesDto {
	name: string;
	defaultMessageTimeToLive: string;
	requiresDuplicateDetection: boolean;
	duplicateDetectionHistoryTimeWindow: string;
	enableBatchedOperations: boolean;
	enablePartitioning: boolean;
	autoDeleteOnIdle: string;
}

export interface SqlSubscriptionRuleDto {
	kind: 'Sql';
	name: string;
	sqlExpression: string;
	actionExpression: string | null;
}

export interface CorrelationSubscriptionRuleDto {
	kind: 'Correlation';
	name: string;
	correlationId: string | null;
	messageId: string | null;
	to: string | null;
	replyTo: string | null;
	subject: string | null;
	sessionId: string | null;
	replyToSessionId: string | null;
	contentType: string | null;
	applicationProperties: Record<string, unknown>;
	actionExpression: string | null;
}

export interface UnknownSubscriptionRuleDto {
	kind: 'Unknown';
	name: string;
	filterTypeName: string;
	filterExpression: string;
}

export type SubscriptionRuleDto =
	| SqlSubscriptionRuleDto
	| CorrelationSubscriptionRuleDto
	| UnknownSubscriptionRuleDto
	| Record<string, unknown>;

export interface SubscriptionEntityPropertiesDto {
	name: string;
	topicName: string;
	lockDuration: string;
	maxDeliveryCount: number;
	defaultMessageTimeToLive: string;
	deadLetteringOnMessageExpiration: boolean;
	requiresSession: boolean;
	enableBatchedOperations: boolean;
	autoDeleteOnIdle: string;
	rules: SubscriptionRuleDto[];
}

export type EntityPropertiesDto =
	| QueueEntityPropertiesDto
	| TopicEntityPropertiesDto
	| SubscriptionEntityPropertiesDto
	| Record<string, unknown>;
