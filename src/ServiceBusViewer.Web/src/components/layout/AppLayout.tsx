import { useEffect, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cx } from '../../lib/cx';
import { useTheme } from '../../hooks/useTheme';

interface HeaderStatus {
  text: string;
  tone: 'connected' | 'ready';
  value?: string | null;
}

type HeaderAction =
  | {
      busy?: boolean;
      disabled?: boolean;
      icon: string;
      kind: 'button';
      label: string;
      onClick: () => Promise<void> | void;
    }
  | {
      icon: string;
      kind: 'link';
      label: string;
      to: string;
    };

interface AppLayoutProps {
  applicationVersion: string;
  children: ReactNode;
  footerStatus?: string;
  headerAction?: HeaderAction;
  headerStatus: HeaderStatus;
  title: string;
}

const statusClasses = {
  connected: 'bg-emerald-500',
  ready: 'bg-amber-500',
} as const;

export function AppLayout({
  applicationVersion,
  children,
  footerStatus,
  headerAction,
  headerStatus,
  title,
}: AppLayoutProps) {
  const { isDark, toggleTheme } = useTheme();

  useEffect(() => {
    document.title = title.length === 0 ? 'Service Bus Viewer' : `${title} | Service Bus Viewer`;
  }, [title]);

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-12 items-center justify-between border-b border-slate-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
        <div className="flex h-full min-w-0 items-center">
          <div className="relative flex h-full items-center px-4 lg:w-64 lg:shrink-0">
            <Link
              to="/"
              className="flex h-full items-center gap-2 rounded px-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
            >
              <img src="/images/app-logo.png" alt="" className="h-7 w-7 shrink-0 object-contain" />
              <div className="text-sm font-bold tracking-tight">Service Bus Viewer</div>
            </Link>
            <div className="pointer-events-none absolute right-0 top-2 bottom-2 hidden w-px bg-slate-200/60 dark:bg-zinc-800/60 lg:block" />
          </div>

          <div className="hidden h-full min-w-0 items-center gap-2 px-4 text-xs leading-none text-slate-500 dark:text-slate-400 sm:flex">
            <span className={cx('h-2 w-2 rounded-full', statusClasses[headerStatus.tone])} />
            <span>{headerStatus.text}</span>
            {headerStatus.value ? (
              <span className="font-mono text-slate-700 dark:text-slate-200">{headerStatus.value}</span>
            ) : null}
          </div>
        </div>

        <div className="flex items-center gap-2 px-4">
          <button
            type="button"
            className="inline-flex items-center justify-center rounded-lg p-1.5 text-slate-600 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-slate-300 dark:hover:bg-zinc-800 dark:hover:text-white"
            aria-label="Toggle color theme"
            aria-pressed={isDark}
            onClick={toggleTheme}
          >
            <span className="material-icons-round text-[20px]">{isDark ? 'light_mode' : 'dark_mode'}</span>
          </button>

          {headerAction?.kind === 'button' ? (
            <button
              type="button"
              className="inline-flex items-center gap-1 rounded-lg border border-rose-200 bg-rose-50 px-3 py-1 text-xs font-medium text-rose-600 transition hover:bg-rose-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 dark:border-rose-900/50 dark:bg-rose-900/20 dark:text-rose-400"
              disabled={headerAction.disabled || headerAction.busy}
              onClick={() => {
                void headerAction.onClick();
              }}
            >
              <span className="material-icons-round text-sm">{headerAction.icon}</span>
              {headerAction.busy ? `${headerAction.label}...` : headerAction.label}
            </button>
          ) : null}

          {headerAction?.kind === 'link' ? (
            <Link
              to={headerAction.to}
              className="inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-slate-200 dark:hover:bg-zinc-800"
            >
              <span className="material-icons-round text-sm">{headerAction.icon}</span>
              {headerAction.label}
            </Link>
          ) : null}
        </div>
      </header>

      {children}

      <footer className="flex h-12 items-center justify-between border-t border-slate-200 bg-white/75 px-4 text-[11px] text-slate-500 backdrop-blur dark:border-slate-800 dark:bg-slate-950/80 dark:text-slate-400">
        <div className="flex items-center gap-1.5">
          <span className="inline-flex items-center leading-none">Service Bus Viewer {applicationVersion}</span>
          <span className="inline-flex items-center leading-none" aria-hidden="true">
            |
          </span>
          <span className="inline-flex items-center leading-none">
            <span className="material-icons-round text-sm leading-none">copyright</span>
          </span>
          <span className="inline-flex items-center leading-none">2026 Andrey Veselov</span>
          <a
            href="https://github.com/VeselovAndrey/ServiceBusViewer"
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center justify-center rounded-lg p-1 text-slate-500 transition hover:bg-slate-200 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:hover:bg-slate-800 dark:hover:text-white"
            aria-label="Open GitHub repository"
          >
            <svg className="h-4 w-4" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M12 2C6.48 2 2 6.58 2 12.22c0 4.5 2.87 8.32 6.84 9.66.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.45-1.18-1.11-1.49-1.11-1.49-.91-.64.07-.63.07-.63 1 .07 1.53 1.05 1.53 1.05.9 1.57 2.36 1.12 2.93.85.09-.67.35-1.12.63-1.38-2.22-.26-4.55-1.14-4.55-5.08 0-1.12.39-2.03 1.03-2.75-.1-.26-.45-1.31.1-2.72 0 0 .84-.28 2.75 1.05A9.32 9.32 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.33 2.75-1.05 2.75-1.05.55 1.41.2 2.46.1 2.72.64.72 1.03 1.63 1.03 2.75 0 3.95-2.33 4.82-4.56 5.08.36.32.68.95.68 1.92 0 1.39-.01 2.5-.01 2.84 0 .27.18.59.69.49A10.24 10.24 0 0 0 22 12.22C22 6.58 17.52 2 12 2Z" />
            </svg>
          </a>
        </div>
        {footerStatus ? <div className="font-mono">{footerStatus}</div> : null}
      </footer>
    </div>
  );
}
