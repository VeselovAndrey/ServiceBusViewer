import { useEffect, useState } from 'react';

function colorSchemeMeta(): HTMLMetaElement {
  return document.querySelector('meta[name="color-scheme"]')!;
}

function systemPrefersDark(): boolean {
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function currentIsDark(): boolean {
  const content = colorSchemeMeta().content;
  if (content === 'dark') {
    return true;
  }
  if (content === 'light') {
    return false;
  }
  return systemPrefersDark();
}

export function useTheme() {
  const [isDark, setIsDark] = useState<boolean>(currentIsDark);

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    // currentIsDark() already accounts for a stored override via the meta
    // content, so no early return on stored values is needed.
    const handleChange = () => setIsDark(currentIsDark());

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, []);

  const toggleTheme = () => {
    const nextIsDark = !currentIsDark();

    if (nextIsDark === systemPrefersDark()) {
      // Target matches the system preference: undo the adjustment so the app
      // follows the system again instead of pinning the scheme.
      window.localStorage.removeItem('theme');
      colorSchemeMeta().content = 'light dark';
    } else {
      const target = nextIsDark ? 'dark' : 'light';
      window.localStorage.setItem('theme', target);
      colorSchemeMeta().content = target;
    }

    setIsDark(nextIsDark);
  };

  return { isDark, toggleTheme };
}
