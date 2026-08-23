import {
	useEffect,
	useRef,
	useState,
	type Dispatch,
	type SetStateAction,
} from 'react';

function readStoredBoolean(key: string, defaultValue: boolean): boolean {
	try {
		const storedValue = window.localStorage.getItem(key);
		return storedValue === null ? defaultValue : storedValue === 'true';
	} catch {
		return defaultValue;
	}
}

export function useStoredBoolean(key: string, defaultValue: boolean) {
	const [value, setValue] = useState<boolean>(() =>
		readStoredBoolean(key, defaultValue),
	);
	const hasChangedRef = useRef(false);

	useEffect(() => {
		if (!hasChangedRef.current) {
			return;
		}
		try {
			window.localStorage.setItem(key, value ? 'true' : 'false');
		} catch {
			// Ignore storage write failures.
		}
	}, [key, value]);

	const setAndPersist: Dispatch<SetStateAction<boolean>> = (nextValue) => {
		hasChangedRef.current = true;
		setValue(nextValue);
	};

	return [value, setAndPersist] as const;
}
