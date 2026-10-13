export const PEEK_MESSAGE_COUNT_MIN = 1;
export const PEEK_MESSAGE_COUNT_MAX = 500;
export const PEEK_MESSAGE_COUNT_DEFAULT = 50;
export const PEEK_MESSAGE_COUNT_PRESETS = [1, 5, 10, 30, 50, 100] as const;
export const PEEK_MESSAGE_COUNT_STORAGE_KEY = 'peekMessageCount';

/**
 * Parses a peek-message-count input into a whole number within the allowed bounds,
 * or returns null when the input is not a whole number from 1 through 500.
 */
export function parsePeekMessageCount(rawValue: string): number | null {
	const trimmed = rawValue.trim();
	if (!/^\d+$/.test(trimmed)) {
		return null;
	}

	const value = Number(trimmed);
	return value >= PEEK_MESSAGE_COUNT_MIN && value <= PEEK_MESSAGE_COUNT_MAX ? value : null;
}

/** Reads the persisted peek message count from browser-local storage, falling back to the default. */
export function readStoredPeekMessageCount(): number {
	try {
		const storedValue = window.localStorage.getItem(PEEK_MESSAGE_COUNT_STORAGE_KEY);
		if (storedValue === null) {
			return PEEK_MESSAGE_COUNT_DEFAULT;
		}

		return parsePeekMessageCount(storedValue) ?? PEEK_MESSAGE_COUNT_DEFAULT;
	} catch {
		return PEEK_MESSAGE_COUNT_DEFAULT;
	}
}
