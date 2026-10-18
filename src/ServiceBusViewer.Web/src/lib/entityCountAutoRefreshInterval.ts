export const ENTITY_COUNT_AUTO_REFRESH_INTERVAL_MIN = 1;
export const ENTITY_COUNT_AUTO_REFRESH_INTERVAL_MAX = 3600;
export const ENTITY_COUNT_AUTO_REFRESH_INTERVAL_DEFAULT = 30;
export const ENTITY_COUNT_AUTO_REFRESH_INTERVAL_PRESETS = [1, 3, 5, 10, 15, 30, 60] as const;
export const ENTITY_COUNT_AUTO_REFRESH_INTERVAL_STORAGE_KEY = 'entityCountAutoRefreshIntervalSeconds';

/**
 * Parses an auto-refresh interval input into a whole number of seconds within the allowed bounds,
 * or returns null when the input is not a whole number from 1 through 3600.
 */
export function parseEntityCountAutoRefreshInterval(rawValue: string): number | null {
	const trimmed = rawValue.trim();
	if (!/^\d+$/.test(trimmed)) {
		return null;
	}

	const value = Number(trimmed);
	return value >= ENTITY_COUNT_AUTO_REFRESH_INTERVAL_MIN && value <= ENTITY_COUNT_AUTO_REFRESH_INTERVAL_MAX
		? value
		: null;
}

/** Reads the persisted auto-refresh interval (in seconds) from browser-local storage, falling back to the default. */
export function readStoredEntityCountAutoRefreshInterval(): number {
	try {
		const storedValue = window.localStorage.getItem(ENTITY_COUNT_AUTO_REFRESH_INTERVAL_STORAGE_KEY);
		if (storedValue === null) {
			return ENTITY_COUNT_AUTO_REFRESH_INTERVAL_DEFAULT;
		}

		return parseEntityCountAutoRefreshInterval(storedValue) ?? ENTITY_COUNT_AUTO_REFRESH_INTERVAL_DEFAULT;
	} catch {
		return ENTITY_COUNT_AUTO_REFRESH_INTERVAL_DEFAULT;
	}
}
