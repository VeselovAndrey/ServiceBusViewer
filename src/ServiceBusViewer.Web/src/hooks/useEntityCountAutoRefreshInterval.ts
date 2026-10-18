import {
	useEffect,
	useRef,
	useState,
	type Dispatch,
	type SetStateAction,
} from 'react';
import {
	ENTITY_COUNT_AUTO_REFRESH_INTERVAL_STORAGE_KEY,
	readStoredEntityCountAutoRefreshInterval,
} from '../lib/entityCountAutoRefreshInterval';

/** Tracks the selected entity count auto-refresh interval (in seconds) and persists it in browser-local storage. */
export function useEntityCountAutoRefreshInterval() {
	const [value, setValue] = useState<number>(() => readStoredEntityCountAutoRefreshInterval());
	const hasChangedRef = useRef(false);

	useEffect(() => {
		if (!hasChangedRef.current) {
			return;
		}

		try {
			window.localStorage.setItem(ENTITY_COUNT_AUTO_REFRESH_INTERVAL_STORAGE_KEY, String(value));
		} catch {
			// Ignore storage write failures.
		}
	}, [value]);

	const setAndPersist: Dispatch<SetStateAction<number>> = (nextValue) => {
		hasChangedRef.current = true;
		setValue(nextValue);
	};

	return [value, setAndPersist] as const;
}
