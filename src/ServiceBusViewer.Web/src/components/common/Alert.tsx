import { cx } from '../../lib/cx';

interface AlertProps {
  className?: string;
  messages: string[];
  tone: 'error' | 'success';
}

const toneClasses = {
  error:
    'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900/70 dark:bg-rose-950/40 dark:text-rose-200',
  success:
    'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900/70 dark:bg-emerald-950/40 dark:text-emerald-200',
} as const;

const iconMarkup = {
  error: (
    <svg
      className="mt-0.5 h-5 w-5 shrink-0"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="9" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v5m0 3h.01" />
    </svg>
  ),
  success: (
    <svg
      className="mt-0.5 h-5 w-5 shrink-0"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      aria-hidden="true"
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M7.5 12.5 10.5 15.5 16.5 8.5"
      />
      <circle cx="12" cy="12" r="9" />
    </svg>
  ),
} as const;

export function Alert({ className, messages, tone }: AlertProps) {
  if (messages.length === 0) {
    return null;
  }

  return (
    <div className={cx('rounded-2xl border px-4 py-3 text-sm', toneClasses[tone], className)}>
      <div className="flex items-start gap-3">
        {iconMarkup[tone]}
        <div className="space-y-1">
          {messages.map((message, index) => (
            <div key={`${tone}-${index}`}>{message}</div>
          ))}
        </div>
      </div>
    </div>
  );
}
