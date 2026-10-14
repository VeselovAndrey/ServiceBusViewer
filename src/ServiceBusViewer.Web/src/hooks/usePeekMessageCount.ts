import {
	useEffect,
	useRef,
	useState,
	type Dispatch,
	type SetStateAction,
} from 'react';
import {
	PEEK_MESSAGE_COUNT_STORAGE_KEY,
	readStoredPeekMessageCount,
} from '../lib/peekMessageCount';

/** Tracks the selected peek message count and persists it in browser-local storage. */
export function usePeekMessageCount() {
	const [value, setValue] = useState<number>(() => readStoredPeekMessageCount());
	const hasChangedRef = useRef(false);

	useEffect(() => {
		if (!hasChangedRef.current) {
			return;
		}

		try {
			window.localStorage.setItem(PEEK_MESSAGE_COUNT_STORAGE_KEY, String(value));
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
