import type { ReceivedMessageDto } from '../types/serviceBus';
import { formatUnknownValue } from './formatters';

/**
 * Returns true when the search text occurs case-insensitively in the message ID, the partition key
 * (only when present), the body, or any application property name or value. Application property
 * values are compared through the same string representation the viewer uses to display them. An
 * empty or whitespace-only search text matches every message.
 */
export function matchesPeekedMessageSearch(message: ReceivedMessageDto, searchText: string): boolean {
	const needle = searchText.trim().toLowerCase();
	if (needle.length === 0) {
		return true;
	}

	const { properties } = message;
	if (properties.messageId.toLowerCase().includes(needle)) {
		return true;
	}

	if (properties.partitionKey !== null && properties.partitionKey.toLowerCase().includes(needle)) {
		return true;
	}

	if (message.body.toLowerCase().includes(needle)) {
		return true;
	}

	if (message.applicationProperties !== null) {
		for (const [name, property] of Object.entries(message.applicationProperties)) {
			if (name.toLowerCase().includes(needle)) {
				return true;
			}

			if (formatUnknownValue(property.value).toLowerCase().includes(needle)) {
				return true;
			}
		}
	}

	return false;
}

/**
 * Returns a new array with only the input messages matching the search text, preserving the input
 * order and never mutating the input array or any viewer state.
 */
export function filterPeekedMessages(
	messages: ReceivedMessageDto[],
	searchText: string,
): ReceivedMessageDto[] {
	return messages.filter((message) => matchesPeekedMessageSearch(message, searchText));
}
