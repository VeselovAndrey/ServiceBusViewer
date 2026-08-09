import { useEffect, useState } from 'react';

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

  useEffect(() => {
    try {
      window.localStorage.setItem(key, value ? 'true' : 'false');
    } catch {
      // Ignore storage write failures.
    }
  }, [key, value]);

  return [value, setValue] as const;
}
