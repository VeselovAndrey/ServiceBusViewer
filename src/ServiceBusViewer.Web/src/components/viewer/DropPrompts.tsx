import { PromptDialogShell } from './RepublishPrompts';

const dropConfirmClassName =
	'rounded-lg border border-slate-300 bg-white px-4 py-1.5 text-xs font-semibold text-slate-600 transition hover:border-rose-300 hover:bg-rose-50 hover:text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-rose-900 dark:hover:bg-rose-950/30 dark:hover:text-rose-300';

interface DropSingleConfirmPromptProps {
	messageId: string;
	onCancel: () => void;
	onConfirm: () => void;
}

export function DropSingleConfirmPrompt({
	messageId,
	onCancel,
	onConfirm,
}: DropSingleConfirmPromptProps) {
	return (
		<PromptDialogShell
			confirmClassName={dropConfirmClassName}
			confirmLabel="Drop"
			description="This permanently deletes the selected dead-lettered message. The deleted message cannot be recovered or republished."
			onCancel={onCancel}
			onConfirm={onConfirm}
			title="Drop dead-lettered message?"
		>
			<p className="text-xs text-slate-500 dark:text-slate-400">
				Message id:
				<span className="mt-1 block break-all font-mono text-slate-700 dark:text-slate-200">{messageId}</span>
			</p>
		</PromptDialogShell>
	);
}

interface DropAllConfirmPromptProps {
	onCancel: () => void;
	onConfirm: () => void;
	visibleCount: number;
}

export function DropAllConfirmPrompt({
	onCancel,
	onConfirm,
	visibleCount,
}: DropAllConfirmPromptProps) {
	return (
		<PromptDialogShell
			confirmClassName={dropConfirmClassName}
			confirmLabel="Drop All"
			description="This permanently deletes every dead-lettered message in the selected entity's dead-letter queue. The deleted messages cannot be recovered or republished."
			onCancel={onCancel}
			onConfirm={onConfirm}
			title="Drop all dead-lettered messages?"
		>
			<p className="text-xs text-slate-500 dark:text-slate-400">
				{visibleCount} message{visibleCount === 1 ? '' : 's'}{' '}
				{visibleCount === 1 ? 'is' : 'are'} currently visible. The drop applies
				to the entire dead-letter queue, including messages beyond the visible
				page.
			</p>
		</PromptDialogShell>
	);
}
