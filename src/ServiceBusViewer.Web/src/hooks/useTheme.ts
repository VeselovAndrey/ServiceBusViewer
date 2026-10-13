import { useCallback, useEffect, useState } from 'react';

/** Persisted value of localStorage key 'theme'. */
export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'theme';
const SYSTEM: ThemePreference = 'system';

function colorSchemeMeta(): HTMLMetaElement {
  return document.querySelector('meta[name="color-scheme"]')!;
}

function systemPrefersDark(): boolean {
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function readStoredPreference(): ThemePreference {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored;
    }
  } catch {
    // Persistence unavailable (e.g. site data blocked); follow the system.
  }
  // Absent (or unknown) stored values are treated as 'system'.
  return SYSTEM;
}

function applyColorScheme(preference: ThemePreference): void {
  // 'system' restores 'light dark' so the app follows prefers-color-scheme
  // and reacts live to OS changes; 'light'/'dark' pin the scheme.
  colorSchemeMeta().content = preference === SYSTEM ? 'light dark' : preference;
}

function effectiveIsDark(preference: ThemePreference): boolean {
  if (preference === 'dark') {
    return true;
  }
  if (preference === 'light') {
    return false;
  }
  return systemPrefersDark();
}

export function useTheme() {
  const [preference, setPreferenceState] = useState<ThemePreference>(readStoredPreference);
  const [isDark, setIsDark] = useState<boolean>(() => effectiveIsDark(preference));

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    // The effective theme tracks OS changes only while the preference
    // follows the system; pinned selections stay fixed.
    const handleChange = () => {
      if (preference === SYSTEM) {
        setIsDark(systemPrefersDark());
      }
    };

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, [preference]);

  const setPreference = useCallback((next: ThemePreference) => {
    setPreferenceState(next);
    applyColorScheme(next);
    setIsDark(effectiveIsDark(next));
    try {
      window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Persistence unavailable; the selection still applies for this session.
    }
  }, []);

  return { isDark, preference, setPreference };
}
