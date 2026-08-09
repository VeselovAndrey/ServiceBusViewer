import type {
  EntityIdDto,
  EntityType,
  ReceivedMessageDto,
  ViewerState,
} from '../types/serviceBus';

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return 'n/a';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

export function formatDateTimeShort(value: string | null | undefined): string {
  if (!value) {
    return 'n/a';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : date.toLocaleString([], { dateStyle: 'short', timeStyle: 'short' });
}

export function formatNullable(
  value: string | null | undefined,
  fallback = 'n/a',
): string {
  return value && value.trim().length > 0 ? value : fallback;
}

export function formatBoolean(value: boolean): string {
  return value ? 'True' : 'False';
}

export function formatUnknownValue(value: unknown): string {
  if (value === null || value === undefined) {
    return 'n/a';
  }

  if (typeof value === 'string') {
    return value;
  }

  if (typeof value === 'number' || typeof value === 'bigint') {
    return String(value);
  }

  if (typeof value === 'boolean') {
    return formatBoolean(value);
  }

  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

export function buildPeekedMessageSummary(
  messages: ReceivedMessageDto[],
  hasMoreMessages: boolean,
): string {
  return hasMoreMessages
    ? `Showing first ${messages.length}`
    : `${messages.length} total`;
}

export function getEntityKey(
  entity: Pick<EntityIdDto, 'type' | 'name' | 'topicName'>,
): string {
  return `${entity.type}:${entity.topicName ?? ''}:${entity.name}`;
}

export function createMessageRowKey(
  message: ReceivedMessageDto,
  index: number,
): string {
  return `${message.properties.messageId}:${message.properties.enqueuedTimeUtc}:${index}`;
}

export function getViewerEntityMeta(viewer: ViewerState): {
  icon: string;
  subtitle: string;
  typeLabel: EntityType;
} {
  const isSubscription = Boolean(viewer.topicName);
  const isTopic =
    !isSubscription &&
    Boolean(viewer.entityName) &&
    viewer.availableEntities.some(
      (entity) => entity.type === 'Topic' && entity.name === viewer.entityName,
    );

  if (!viewer.entityName) {
    return {
      icon: 'view_headline',
      subtitle:
        'Select a queue or subscription from the left sidebar to load messages.',
      typeLabel: 'Queue',
    };
  }

  if (isSubscription) {
    return {
      icon: 'subtitles',
      subtitle: `Streaming from topic ${viewer.topicName}`,
      typeLabel: 'Subscription',
    };
  }

  if (isTopic) {
    return {
      icon: 'share',
      subtitle: 'Topic publishing surface',
      typeLabel: 'Topic',
    };
  }

  return {
    icon: 'view_headline',
    subtitle: 'Queue messages and operations',
    typeLabel: 'Queue',
  };
}
