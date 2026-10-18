import type { AvailableEntityDto, EntityMessageCountsDto } from '../types/serviceBus';

export interface MessageCountCategory {
  label: 'Active' | 'Scheduled' | 'DLQ';
  value: number;
}

// The active count is always shown; scheduled and DLQ trail it only when they (or a later category) are non-zero.
export function getDisplayedMessageCounts(counts: EntityMessageCountsDto): MessageCountCategory[] {
  const categories: MessageCountCategory[] = [
    { label: 'Active', value: counts.activeMessageCount },
    { label: 'Scheduled', value: counts.scheduledMessageCount },
    { label: 'DLQ', value: counts.deadLetterMessageCount },
  ];

  let lastDisplayedIndex = 0;
  if (counts.deadLetterMessageCount !== 0) {
    lastDisplayedIndex = 2;
  } else if (counts.scheduledMessageCount !== 0) {
    lastDisplayedIndex = 1;
  }

  return categories.slice(0, lastDisplayedIndex + 1);
}

// Aggregates per-category sums across the given entities; null when none of them carry counts.
export function sumMessageCounts(entities: AvailableEntityDto[]): EntityMessageCountsDto | null {
  let activeMessageCount = 0;
  let scheduledMessageCount = 0;
  let deadLetterMessageCount = 0;
  let hasCounts = false;

  for (const entity of entities) {
    if (!entity.messageCounts) {
      continue;
    }

    hasCounts = true;
    activeMessageCount += entity.messageCounts.activeMessageCount;
    scheduledMessageCount += entity.messageCounts.scheduledMessageCount;
    deadLetterMessageCount += entity.messageCounts.deadLetterMessageCount;
  }

  return hasCounts ? { activeMessageCount, scheduledMessageCount, deadLetterMessageCount } : null;
}
