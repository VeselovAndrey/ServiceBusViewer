import { useEffect, useState } from 'react';
import { cx } from '../../lib/cx';

interface CopyButtonProps {
	text: string | null | undefined;
	label: string;
	icon: string;
	copiedText?: string;
	ariaLabel?: string;
	disabled?: boolean;
	disabledTitle?: string;
	className?: string;
}

type CopyStatus = 'idle' | 'copied' | 'failed';

export function CopyButton({
	text,
	label,
	icon,
	copiedText = 'Copied',
	ariaLabel,
	disabled = false,
	disabledTitle,
	className,
}: CopyButtonProps) {
	const [copyStatus, setCopyStatus] = useState<CopyStatus>('idle');

	useEffect(() => {
		if (copyStatus === 'idle') {
			return undefined;
		}

		const timeoutId = window.setTimeout(() => {
			setCopyStatus('idle');
		}, 2000);

		return () => {
			window.clearTimeout(timeoutId);
		};
	}, [copyStatus]);

	const handleClick = async () => {
		if (text === null || text === undefined || !navigator.clipboard?.writeText) {
			setCopyStatus('failed');
			return;
		}

		try {
			await navigator.clipboard.writeText(text);
			setCopyStatus('copied');
		} catch {
			setCopyStatus('failed');
		}
	};

	const isDisabled = disabled || text === null || text === undefined;
	const isBusy = copyStatus !== 'idle';

	return (
		<button
			type="button"
			disabled={isDisabled}
			className={cx(
				'relative inline-flex h-8 items-center justify-center overflow-hidden rounded-lg border text-[11px] font-medium transition-all duration-200',
				isDisabled
					? 'cursor-not-allowed border-slate-200 bg-slate-50 text-slate-400 dark:border-slate-800 dark:bg-slate-950/40 dark:text-slate-600'
					: copyStatus === 'copied'
						? 'border-emerald-300 bg-emerald-50 text-emerald-700 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-400'
						: copyStatus === 'failed'
							? 'border-rose-300 bg-rose-50 text-rose-700 dark:border-rose-900/60 dark:bg-rose-950/30 dark:text-rose-400'
							: 'border-slate-200 bg-white text-slate-700 hover:border-slate-300 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:border-slate-700 dark:hover:bg-slate-800',
				className,
			)}
			onClick={handleClick}
			aria-label={ariaLabel ?? `Copy ${label.toLowerCase()}`}
			title={isDisabled ? (disabledTitle ?? ariaLabel ?? `Copy ${label.toLowerCase()}`) : (ariaLabel ?? `Copy ${label.toLowerCase()}`)}
		>
			<span
				className={cx(
					'flex items-center gap-1.5 transition-all duration-200',
					isBusy ? '-translate-y-1 opacity-0' : 'translate-y-0 opacity-100',
				)}
			>
				<span className="material-icons-round text-sm" aria-hidden="true">
					{icon}
				</span>
				<span>{label}</span>
			</span>
			<span
				aria-live="polite"
				className={cx(
					'absolute inset-0 flex items-center justify-center transition-all duration-200',
					isBusy ? 'translate-y-0 opacity-100' : 'translate-y-1 opacity-0',
				)}
			>
				{copyStatus === 'copied' ? copiedText : 'Copy failed'}
			</span>
		</button>
	);
}
