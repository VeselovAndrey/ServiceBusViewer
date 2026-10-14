import type { ReceivedMessageDto } from '../types/serviceBus';

export type PeekedMessageSortColumn =
	| 'messageId'
	| 'session'
	| 'deliveryCount'
	| 'enqueuedTime'
	| 'contentType'
	| 'deadLetterReason';

export type PeekedMessageSortDirection = 'ascending' | 'descending';

export interface PeekedMessageSort {
	column: PeekedMessageSortColumn;
	direction: PeekedMessageSortDirection;
}

function compareText(left: string | null, right: string | null): number {
	return (left ?? '').localeCompare(right ?? '');
}

function compareEnqueuedTime(left: string, right: string): number {
	const leftTime = Date.parse(left);
	const rightTime = Date.parse(right);
	if (Number.isNaN(leftTime) || Number.isNaN(rightTime)) {
		return left.localeCompare(right);
	}

	return leftTime - rightTime;
}

/**
 * Compares two peeked messages by the given column and direction. Null or empty Session and
 * Content Type values compare as empty text, so they form one contiguous run; equal sort values
 * keep their input order through the caller's stable sort.
 */
export function comparePeekedMessages(
	left: ReceivedMessageDto,
	right: ReceivedMessageDto,
	sort: PeekedMessageSort,
): number {
	let result: number;
	switch (sort.column) {
		case 'messageId':
			result = compareText(left.properties.messageId, right.properties.messageId);
			break;
		case 'session':
			result = compareText(left.properties.sessionId, right.properties.sessionId);
			break;
		case 'deliveryCount':
			result = left.properties.deliveryCount - right.properties.deliveryCount;
			break;
		case 'enqueuedTime':
			result = compareEnqueuedTime(left.properties.enqueuedTimeUtc, right.properties.enqueuedTimeUtc);
			break;
		case 'contentType':
			result = compareText(left.properties.contentType, right.properties.contentType);
			break;
		case 'deadLetterReason':
			result = compareText(left.properties.deadLetterReason, right.properties.deadLetterReason);
			break;
	}

	return sort.direction === 'descending' ? -result : result;
}

/**
 * Returns a new array of the input messages ordered by the given column and direction without
 * mutating the input. A null sort returns a copy in the original loaded order.
 */
export function sortPeekedMessages(
	messages: ReceivedMessageDto[],
	sort: PeekedMessageSort | null,
): ReceivedMessageDto[] {
	if (sort === null) {
		return messages.slice();
	}

	return messages.slice().sort((left, right) => comparePeekedMessages(left, right, sort));
}
