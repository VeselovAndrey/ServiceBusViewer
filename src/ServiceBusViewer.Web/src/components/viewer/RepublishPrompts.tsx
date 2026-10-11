import { useEffect, useState, type ReactNode } from 'react';

interface PromptDialogShellProps {
	confirmClassName?: string;
	confirmDisabled?: boolean;
	confirmLabel?: string;
	description?: string | null;
	onCancel: () => void;
	onConfirm: () => void;
	title: string;
	children: ReactNode;
}

export function PromptDialogShell({
	children,
	confirmClassName = 'rounded-lg bg-brand-600 px-4 py-1.5 text-xs font-semibold text-white transition hover:bg-brand-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:cursor-not-allowed disabled:opacity-60',
	confirmDisabled = false,
	confirmLabel = 'Republish',
	description = null,
	onCancel,
	onConfirm,
	title,
}: PromptDialogShellProps) {
	useEffect(() => {
		const handleKeyDown = (event: KeyboardEvent) => {
			if (event.key === 'Escape') {
				onCancel();
			}
		};

		document.addEventListener('keydown', handleKeyDown);
		return () => document.removeEventListener('keydown', handleKeyDown);
	}, [onCancel]);

	return (
		<div
			className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
			role="presentation"
			onClick={onCancel}
		>
			<div
				aria-label={title}
				aria-modal="true"
				className="w-full max-w-md overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl dark:border-slate-800 dark:bg-slate-900"
				role="dialog"
				onClick={(event) => event.stopPropagation()}
			>
				<div className="border-b border-slate-200 px-5 py-4 dark:border-slate-800">
					<h2 className="text-sm font-semibold text-slate-900 dark:text-white">{title}</h2>
					{description ? (
						<p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{description}</p>
					) : null}
				</div>

				<div className="p-5">{children}</div>

				<div className="flex items-center justify-end gap-2 border-t border-slate-200 bg-slate-50/80 px-5 py-3 dark:border-slate-800 dark:bg-slate-950/40">
					<button
						type="button"
						className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-zinc-800 dark:bg-zinc-900 dark:text-slate-300 dark:hover:bg-zinc-800"
						onClick={onCancel}
					>
						Cancel
					</button>
					<button
						type="button"
						className={confirmClassName}
						disabled={confirmDisabled}
						onClick={onConfirm}
					>
						{confirmLabel}
					</button>
				</div>
			</div>
		</div>
	);
}

interface RepublishManualIdPromptProps {
	messageId: string;
	onCancel: () => void;
	onConfirm: (manualMessageId: string) => void;
}

export function RepublishManualIdPrompt({
	messageId,
	onCancel,
	onConfirm,
}: RepublishManualIdPromptProps) {
	const [value, setValue] = useState('');
	const trimmedValue = value.trim();

	return (
		<PromptDialogShell
			confirmDisabled={trimmedValue.length === 0}
			description="The Manual strategy sends the republished copy under the message id you enter here."
			onCancel={onCancel}
			onConfirm={() => {
				if (trimmedValue.length > 0) {
					onConfirm(trimmedValue);
				}
			}}
			title="Manual message id"
		>
			<p className="text-xs text-slate-500 dark:text-slate-400">
				Current message id:
				<span className="mt-1 block break-all font-mono text-slate-700 dark:text-slate-200">{messageId}</span>
			</p>
			<label
				htmlFor="republish-manual-message-id"
				className="mt-3 block text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500"
			>
				New message id
			</label>
			<input
				id="republish-manual-message-id"
				type="text"
				autoFocus
				maxLength={128}
				className="mt-1 w-full rounded-lg border border-slate-200 bg-white px-3 py-2 font-mono text-xs text-slate-700 transition placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100 dark:placeholder:text-slate-500"
				placeholder="e.g. 3f2b1a4d-9c8e-4f6a-b5d7-2e1c0a9b8d7f"
				value={value}
				onChange={(event) => setValue(event.target.value)}
				onKeyDown={(event) => {
					if (event.key === 'Enter' && trimmedValue.length > 0) {
						onConfirm(trimmedValue);
					}
				}}
			/>
		</PromptDialogShell>
	);
}
