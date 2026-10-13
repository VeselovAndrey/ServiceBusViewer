import { useEffect, useLayoutEffect, useRef, useState, type CSSProperties } from 'react';
import { cx } from '../../lib/cx';
import type { ThemePreference } from '../../hooks/useTheme';

interface ThemePreferenceMenuProps {
  onPick: (preference: ThemePreference) => void;
  preference: ThemePreference;
}

const themeOptions: Array<{ label: string; value: ThemePreference }> = [
  { label: 'Light', value: 'light' },
  { label: 'Dark', value: 'dark' },
  { label: 'Use device settings', value: 'system' },
];

const optionIcons: Record<ThemePreference, string> = {
  dark: 'dark_mode',
  light: 'light_mode',
  system: 'monitor',
};

const EDGE_OFFSET_PX = 4;

export function ThemePreferenceMenu({ onPick, preference }: ThemePreferenceMenuProps) {
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<CSSProperties | null>(null);

  const toggleOpen = () => {
    if (open) {
      setOpen(false);
      return;
    }

    const rect = buttonRef.current?.getBoundingClientRect();
    if (rect === undefined) {
      return;
    }

    // Anchor the menu to the right edge of the button (header control sits
    // at the right side of the bar).
    setPosition({
      top: rect.bottom + EDGE_OFFSET_PX,
      right: window.innerWidth - rect.right,
    });
    setOpen(true);
  };

  // Flip the menu above the button when it would overflow the viewport bottom.
  useLayoutEffect(() => {
    if (!open || position === null || menuRef.current === null) {
      return;
    }

    const menuRect = menuRef.current.getBoundingClientRect();
    if (menuRect.bottom > window.innerHeight - EDGE_OFFSET_PX) {
      const rect = buttonRef.current?.getBoundingClientRect();
      if (rect !== undefined) {
        setPosition({ ...position, top: rect.top - menuRect.height - EDGE_OFFSET_PX });
      }
    }
  }, [open, position]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const close = () => setOpen(false);
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.stopPropagation();
        setOpen(false);
        buttonRef.current?.focus();
      }
    };
    const handlePointerDown = (event: Event) => {
      const target = event.target as Node;
      if (menuRef.current?.contains(target) || buttonRef.current?.contains(target)) {
        return;
      }

      setOpen(false);
    };

    window.addEventListener('scroll', close, true);
    window.addEventListener('resize', close);
    document.addEventListener('keydown', handleKeyDown, true);
    document.addEventListener('pointerdown', handlePointerDown, true);
    return () => {
      window.removeEventListener('scroll', close, true);
      window.removeEventListener('resize', close);
      document.removeEventListener('keydown', handleKeyDown, true);
      document.removeEventListener('pointerdown', handlePointerDown, true);
    };
  }, [open]);

  return (
    <>
      <button
        ref={buttonRef}
        type="button"
        aria-expanded={open}
        aria-haspopup="menu"
        aria-label="Color theme"
        className="inline-flex items-center justify-center rounded-lg p-1.5 text-slate-600 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-slate-300 dark:hover:bg-zinc-800 dark:hover:text-white"
        title="Color theme"
        onClick={toggleOpen}
      >
        <span className="material-icons-round text-[1.25rem]">{optionIcons[preference]}</span>
      </button>

      {open && position !== null ? (
        <div
          ref={menuRef}
          role="menu"
          aria-label="Color theme"
          className="fixed z-50 min-w-44 overflow-hidden rounded-xl border border-slate-200 bg-white py-1.5 shadow-xl dark:border-slate-800 dark:bg-slate-900"
          style={position}
        >
          {themeOptions.map((option) => (
            <button
              key={option.value}
              type="button"
              role="menuitemradio"
              aria-checked={preference === option.value}
              className="flex w-full items-center gap-2.5 px-4 py-2 text-left text-xs font-medium text-slate-700 transition hover:bg-slate-50 focus-visible:bg-slate-50 focus-visible:outline-none dark:text-slate-200 dark:hover:bg-slate-800/60 dark:focus-visible:bg-slate-800/60"
              onClick={() => {
                setOpen(false);
                onPick(option.value);
              }}
            >
              <span className="material-icons-round text-base leading-none" aria-hidden="true">
                {optionIcons[option.value]}
              </span>
              {option.label}
              <span
                className={cx(
                  'ml-auto material-icons-round text-base leading-none',
                  preference === option.value ? 'text-brand-500' : 'invisible',
                )}
                aria-hidden="true"
              >
                check
              </span>
            </button>
          ))}
        </div>
      ) : null}
    </>
  );
}
