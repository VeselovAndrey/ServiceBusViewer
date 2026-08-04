import { useEffect, useState } from 'react';

export function useTheme() {
  const [isDark, setIsDark] = useState<boolean>(() =>
    document.documentElement.classList.contains('dark'),
  );

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    const handleChange = (event: MediaQueryListEvent) => {
      if (window.localStorage.getItem('theme')) {
        return;
      }

      document.documentElement.classList.toggle('dark', event.matches);
      setIsDark(event.matches);
    };

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, []);

  const toggleTheme = () => {
    const nextValue = !document.documentElement.classList.contains('dark');
    document.documentElement.classList.toggle('dark', nextValue);
    window.localStorage.setItem('theme', nextValue ? 'dark' : 'light');
    setIsDark(nextValue);
  };

  return { isDark, toggleTheme };
}
